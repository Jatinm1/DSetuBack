using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.DirectoryServices;

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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<LoginService> _logger;


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
            DealerSetu.Repository.Common.Utility utility,
            IHttpContextAccessor httpContextAccessor,
            ILogger<LoginService> logger)
        {
            try
            {
                _loginRepo = loginRepo ?? throw new ArgumentNullException(nameof(loginRepo));
                _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
                _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
                _utility = utility ?? throw new ArgumentNullException(nameof(utility));
                _httpContextAccessor = httpContextAccessor;
                _logger = logger;
            }
            catch
            {
                throw new ArgumentNullException("Service initialization failed");
            }
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
            try
            {
                string escapedUserName = _utility.EscapeLDAPUsername(userName);
                if (string.IsNullOrWhiteSpace(escapedUserName) || string.IsNullOrWhiteSpace(password))
                    return false;

                // Get the current domain user (e.g., DOMAIN\username).
                string domainUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                // Split the domain user string into two parts: domain and username.
                string[] paramsLogin = domainUser.Split('\\');

                // Extract the domain name (e.g., DOMAIN)
                string domainName = paramsLogin.Length > 0 ? paramsLogin[0] : string.Empty;

                // Combine domain and username (e.g., DOMAIN\username)
                string domainAndUsername = $"{domainName}\\{escapedUserName}";

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
                        using (DirectoryEntry entry = new DirectoryEntry(ldapPath, escapedUserName, password))
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
            try
            {
                if (filter == null)
                {
                    return CreateErrorResponse("Filter cannot be null", "400");
                }

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
            catch
            {
                return CreateErrorResponse("Service temporarily unavailable", "500");
            }
        }

        /// <summary>
        /// Performs standard login authentication for local/staging environments.
        /// </summary>
        /// <param name="loginModel">Login credentials containing username and password</param>
        /// <returns>Service response containing user information and JWT token or error details</returns>
        public async Task<ServiceResponse> Login_Service(LoginModel loginModel)
        {
            try
            {
                var validationResult = ValidateLoginModel(loginModel);
                if (!validationResult.IsValid)
                {
                    return validationResult.Response;
                }

                var result = await _loginRepo.LoginRepo(loginModel);

                return await ProcessLoginResult(result, loginModel);
            }
            catch
            {
                return CreateErrorResponse(
                    "Username or Password is Incorrect, please try again",
                    "400");
            }
        }

        /// <summary>
        /// Performs LDAP-based login authentication for production environments.
        /// </summary>
        /// <param name="loginModel">Login credentials containing username and password</param>
        /// <returns>Service response containing user information and JWT token or error details</returns>
        //public async Task<ServiceResponse> LDAPLoginService(LoginModel loginModel)
        //{
        //    try
        //    {
        //        var validationResult = ValidateLoginModel(loginModel);
        //        if (!validationResult.IsValid)
        //        {
        //            return validationResult.Response;
        //        }

        //        var ldapAuth = await isLDAPAuthAsync(loginModel.EmpNo, loginModel.Password);

        //        if (!ldapAuth)
        //        {
        //            return CreateErrorResponse("Unauthorized Access", "401");
        //        }

        //        var result = await _loginRepo.LDAPLoginRepo(loginModel, true);

        //        return await ProcessLoginResult(result);
        //    }
        //    catch
        //    {
        //        return CreateErrorResponse(
        //            "System cannot find the combination of this username and password, please try again",
        //            "400"); 
        //    }
        //}

        /// <summary>
        /// Logs out a user by their employee number.
        /// </summary>
        /// <param name="empNo">Employee number of the user to logout</param>
        /// <returns>Service response indicating success or failure of logout operation</returns>
        public async Task<ServiceResponse> LogOutService(string empNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empNo))
                {
                    return CreateErrorResponse("Employee number is required", "400");
                }

                var logoutResult = await _loginRepo.LogoutRepo(empNo);

                // Clear all cookies - both HTTP-only and regular cookies
                ClearAllAuthCookies();

                return new ServiceResponse
                {
                    Status = logoutResult.Status,
                    Code = logoutResult.Code,
                    Message = logoutResult.Message,
                    isError = logoutResult.Code != "200"
                };
            }
            catch
            {
                // Still clear cookies even if database operation fails
                ClearAllAuthCookies();

                return CreateErrorResponse("Service temporarily unavailable", "500");
            }
        }

        /// <summary>
        /// Updates the login heartbeat for a specific employee to track active sessions.
        /// </summary>
        /// <param name="empNo">Employee number to update heartbeat for</param>
        /// <returns>True if heartbeat update was successful, false otherwise</returns>
        public async Task<bool> UpdateLoginHeartbeatService(string empNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(empNo))
                    return false;

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
            try
            {
                if (string.IsNullOrWhiteSpace(empNo))
                    return false;

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
            try
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
            catch
            {
                return (false, CreateErrorResponse("Validation failed", "400"));
            }
        }

        /// <summary>
        /// Processes the login result from repository and creates appropriate service response.
        /// </summary>
        /// <param name="result">Result from login repository operation</param>
        /// <returns>Service response with user information and JWT token</returns>
        private async Task<ServiceResponse> ProcessLoginResult(dynamic result, LoginModel model)
        {
            try
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
                        Role = result.Role,
                        BrowserName = model.BrowserName,
                        BrowserVersion = model.BrowserVersion,
                        IpAddress = model.IpAddress
                    };

                    // Generate JWT token
                    string token = _jwtTokenGenerator.GenerateJsonWebToken(tokenHelperModel, _configuration);
                    await _loginRepo.UpdateUserAndGenerateTokenAsync(tokenHelperModel, token);

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
            catch
            {
                return CreateErrorResponse("Service temporarily unavailable", "500");
            }
        }

        private CookieOptions GetJwtCookieOptions(bool expired = false)
        {
            try
            {
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/",
                    Domain = null
                };

                if (expired)
                {
                    options.Expires = DateTime.UtcNow.AddDays(-1);
                }

                return options;
            }
            catch
            {
                return new CookieOptions();
            }
        }

        private void ClearAllAuthCookies()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext != null)
                {
                    // Log current cookies for debugging
                    var existingJwt = httpContext.Request.Cookies["jwt"];
                    //_logger.LogInformation($"JWT cookie exists before clearing: {!string.IsNullOrEmpty(existingJwt)}");

                    // STRATEGY 1: Try multiple domain variations
                    var domainVariations = new string[] { null, "", httpContext.Request.Host.Host };

                    foreach (var domain in domainVariations)
                    {
                        // Method 1: Set expired cookie
                        httpContext.Response.Cookies.Append("jwt", "", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.None,
                            Path = "/",
                            Domain = domain,
                            Expires = DateTime.UtcNow.AddYears(-1)
                        });

                        // Method 2: Delete cookie
                        httpContext.Response.Cookies.Delete("jwt", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = SameSiteMode.None,
                            Path = "/",
                            Domain = domain
                        });
                    }

                    // STRATEGY 2: Also try without Secure flag (in case of mixed HTTP/HTTPS)
                    httpContext.Response.Cookies.Append("jwt", "", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false, // Try without secure
                        SameSite = SameSiteMode.None,
                        Path = "/",
                        Expires = DateTime.UtcNow.AddYears(-1)
                    });

                    httpContext.Response.Cookies.Delete("jwt", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.None,
                        Path = "/"
                    });

                    // STRATEGY 3: Try different SameSite values
                    var sameSiteValues = new[] { SameSiteMode.None, SameSiteMode.Lax, SameSiteMode.Strict };
                    foreach (var sameSite in sameSiteValues)
                    {
                        httpContext.Response.Cookies.Delete("jwt", new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = sameSite,
                            Path = "/"
                        });
                    }

                    // Clear other cookies (existing code)
                    ClearRegularCookies(httpContext);
                }
            }
            catch
            {
                // Silent fail for cookie clearing
            }
        }

        private void ClearRegularCookies(HttpContext httpContext)
        {
            try
            {
                var regularCookieOptions = new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                };

                httpContext.Response.Cookies.Delete("isAuthenticated", regularCookieOptions);
                httpContext.Response.Cookies.Delete("empNo", regularCookieOptions);
                httpContext.Response.Cookies.Delete("userName", regularCookieOptions);
                httpContext.Response.Cookies.Delete("userRole", regularCookieOptions);
                httpContext.Response.Cookies.Delete("lastActivity", regularCookieOptions);
            }
            catch
            {
                // Silent fail for cookie clearing
            }
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