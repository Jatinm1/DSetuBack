using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Configuration;
using System.DirectoryServices;
using System.ComponentModel.DataAnnotations;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Service responsible for handling user authentication, login operations, and session management.
    /// Supports both local/staging authentication and LDAP-based production authentication.
    /// </summary>
    public class LoginService : ILoginService
    {
        #region Private Fields

        private readonly ILoginRepository _loginRepo;
        private readonly IConfiguration _configuration;
        private readonly DealerSetu.Repository.Common.Utility _utility;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the LoginService with required dependencies.
        /// </summary>
        /// <param name="loginRepo">Repository for login operations</param>
        /// <param name="jwtTokenGenerator">Service for generating JWT tokens</param>
        /// <param name="configuration">Application configuration</param>
        /// <param name="utility">Utility service for common operations</param>
        /// <exception cref="ArgumentNullException">Thrown when any required parameter is null</exception>
        public LoginService(
            ILoginRepository loginRepo,
            JwtTokenGenerator jwtTokenGenerator,
            IConfiguration configuration,
            DealerSetu.Repository.Common.Utility utility)
        {
            _loginRepo = loginRepo ?? throw new ArgumentNullException(nameof(loginRepo));
            _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Authenticates user credentials against LDAP directory service.
        /// </summary>
        /// <param name="userName">Username for LDAP authentication</param>
        /// <param name="password">Password for LDAP authentication</param>
        /// <returns>True if authentication is successful, false otherwise</returns>
        public async Task<bool> isLDAPAuthAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                // Get the current domain user (e.g., DOMAIN\username).
                string domainUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                // Split the domain user string into two parts: domain and username.
                string[] paramsLogin = domainUser.Split('\\');

                // Extract the domain name (e.g., DOMAIN)
                string domainName = paramsLogin.Length > 0 ? paramsLogin[0] : string.Empty;

                // Combine domain and username (e.g., DOMAIN\username)
                string domainAndUsername = $"{domainName}\\{userName}";

                // Set the LDAP path 
                string ldapPath = _configuration["ldapConfiguration:ADPath"];

                if (string.IsNullOrWhiteSpace(ldapPath))
                    return false;

                // Create a DirectoryEntry to connect to the LDAP directory
                // Wrap in Task.Run to make this CPU-bound operation asynchronous
                bool isValid = await Task.Run(() =>
                {
                    try
                    {
                        using (DirectoryEntry entry = new DirectoryEntry(ldapPath, userName, password))
                        {
                            return !string.IsNullOrEmpty(entry.Name);
                        }
                    }
                    catch
                    {
                        return false;
                    }
                });

                return isValid;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves pending counts for a specific employee based on their role.
        /// </summary>
        /// <param name="filter">Filter containing employee number and role ID</param>
        /// <returns>Service response containing pending counts or error information</returns>
        public async Task<ServiceResponse> PendingCountService(FilterModel filter)
        {
            if (filter == null)
            {
                return CreateErrorResponse("Filter cannot be null", "400");
            }

            try
            {
                var pendingCounts = await _loginRepo.PendingCountRepo(filter.EmpNo, filter.RoleId);

                return new ServiceResponse
                {
                    isError = false,
                    result = pendingCounts,
                    Message = "Pending counts retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error retrieving pending counts: {ex.Message}", "500", ex.Message);
            }
        }

        /// <summary>
        /// Performs standard login authentication for local/staging environments.
        /// </summary>
        /// <param name="loginModel">Login credentials containing username and password</param>
        /// <returns>Service response containing user information and JWT token or error details</returns>
        public async Task<ServiceResponse> Login_Service(LoginModel loginModel)
        {
            var validationResult = ValidateLoginModel(loginModel);
            if (!validationResult.IsValid)
            {
                return validationResult.Response;
            }

            try
            {
                var result = await _loginRepo.LoginRepo(loginModel);

                return await ProcessLoginResult(result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(
                    "System cannot find the combination of this username and password, please try again",
                    "400",
                    ex.Message);
            }
        }

        /// <summary>
        /// Performs LDAP-based login authentication for production environments.
        /// </summary>
        /// <param name="loginModel">Login credentials containing username and password</param>
        /// <returns>Service response containing user information and JWT token or error details</returns>
        public async Task<ServiceResponse> LDAPLoginService(LoginModel loginModel)
        {
            var validationResult = ValidateLoginModel(loginModel);
            if (!validationResult.IsValid)
            {
                return validationResult.Response;
            }

            try
            {
                var ldapAuth = await isLDAPAuthAsync(loginModel.EmpNo, loginModel.Password);

                if (!ldapAuth)
                {
                    return CreateErrorResponse("Unauthorized Access", "401");
                }

                var result = await _loginRepo.LDAPLoginRepo(loginModel, true);

                return await ProcessLoginResult(result);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(
                    "System cannot find the combination of this username and password, please try again",
                    "400",
                    ex.Message);
            }
        }

        /// <summary>
        /// Logs out a user by their employee number.
        /// </summary>
        /// <param name="empNo">Employee number of the user to logout</param>
        /// <returns>Service response indicating success or failure of logout operation</returns>
        public async Task<ServiceResponse> LogOutService(string empNo)
        {
            if (string.IsNullOrWhiteSpace(empNo))
            {
                return CreateErrorResponse("Employee number is required", "400");
            }

            try
            {
                var logoutResult = await _loginRepo.LogoutRepo(empNo);

                return new ServiceResponse
                {
                    Status = logoutResult.Status,
                    Code = logoutResult.Code,
                    Message = logoutResult.Message,
                    isError = logoutResult.Code != "200"
                };
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Failed to logout: {ex.Message}", "500", ex.Message);
            }
        }

        /// <summary>
        /// Updates the login heartbeat for a specific employee to track active sessions.
        /// </summary>
        /// <param name="empNo">Employee number to update heartbeat for</param>
        /// <returns>True if heartbeat update was successful, false otherwise</returns>
        public async Task<bool> UpdateLoginHeartbeatService(string empNo)
        {
            if (string.IsNullOrWhiteSpace(empNo))
                return false;

            try
            {
                return await _loginRepo.UpdateLoginHeartBeatRepo(empNo);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Updates the regular heartbeat for a specific employee to track system activity.
        /// </summary>
        /// <param name="empNo">Employee number to update heartbeat for</param>
        /// <returns>True if heartbeat update was successful, false otherwise</returns>
        public async Task<bool> UpdateRegularHeartbeatService(string empNo)
        {
            if (string.IsNullOrWhiteSpace(empNo))
                return false;

            try
            {
                return await _loginRepo.UpdateRegularHeartBeatRepo(empNo);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates the login model for required fields and format.
        /// </summary>
        /// <param name="loginModel">Login model to validate</param>
        /// <returns>Validation result with success status and response if invalid</returns>
        private (bool IsValid, ServiceResponse Response) ValidateLoginModel(LoginModel loginModel)
        {
            if (loginModel == null)
            {
                return (false, CreateErrorResponse("Login model cannot be null", "400"));
            }

            if (string.IsNullOrWhiteSpace(loginModel.EmpNo) || string.IsNullOrWhiteSpace(loginModel.Password))
            {
                return (false, CreateErrorResponse("Username and password are required", "400"));
            }

            if (!_utility.IsValidUsername(loginModel.EmpNo))
            {
                return (false, CreateErrorResponse("Invalid username format", "400"));
            }

            return (true, null);
        }

        /// <summary>
        /// Processes the login result from repository and creates appropriate service response.
        /// </summary>
        /// <param name="result">Result from login repository operation</param>
        /// <returns>Service response with user information and JWT token</returns>
        private async Task<ServiceResponse> ProcessLoginResult(dynamic result)
        {
            var response = new ServiceResponse
            {
                Status = result.Status,
                Code = result.Code,
                Message = result.Message,
                isError = result.Code != "200"
            };

            if (result.Code == "200")
            {
                var tokenHelperModel = new TokenHelperModel
                {
                    UserName = result.Name,
                    EmpNo = result.EmpOrDNo,
                    UserId = result.UserId,
                    RoleId = result.RoleId,
                    Role = result.Role
                };

                // Generate JWT token
                string token = _jwtTokenGenerator.GenerateJsonWebToken(tokenHelperModel, _configuration);

                var userViewModel = new UserViewModel
                {
                    UserName = result.Name,
                    EmpNo = result.EmpOrDNo,
                    Role = result.Role,
                    RoleId = result.RoleId,
                    Token = token
                };

                response.result = userViewModel;
            }

            return response;
        }

        /// <summary>
        /// Creates a standardized error response.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code</param>
        /// <param name="error">Detailed error information (optional)</param>
        /// <returns>ServiceResponse with error details</returns>
        private static ServiceResponse CreateErrorResponse(string message, string code, string error = null)
        {
            return new ServiceResponse
            {
                Status = "Failure",
                Code = code,
                Message = message,
                isError = true,
                Error = error
            };
        }

        #endregion
    }
}