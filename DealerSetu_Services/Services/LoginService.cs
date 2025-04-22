using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Configuration;
using System.DirectoryServices;

namespace DealerSetu_Services.Services
{
    public class LoginService : ILoginService
    {
        private readonly ILoginRepository _loginRepo;
        private readonly IReportRepository _loinRepo;
        private readonly IConfiguration _configuration;
        private readonly DealerSetu.Repository.Common.Utility _utility;
        private readonly JwtTokenGenerator _jwtTokenGenerator;

        public LoginService(ILoginRepository loginRepo, JwtTokenGenerator jwtTokenGenerator, IConfiguration configuration, DealerSetu.Repository.Common.Utility utility)
        {
            _loginRepo = loginRepo;
            _jwtTokenGenerator = jwtTokenGenerator;
            _configuration = configuration;
            _utility = utility;
        }

        public async Task<bool> isLDAPAuthAsync(string UserName, string Password)
        {
            try
            {
                // Get the current domain user (e.g., DOMAIN\username).
                string domainUser = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                // Split the domain user string into two parts: domain and username.
                string[] paramsLogin = domainUser.Split('\\');

                // Extract the domain name (e.g., DOMAIN)
                string domainName = paramsLogin[0].ToString();

                // Combine domain and username (e.g., DOMAIN\username)
                string domainAndUsername = domainName + @"\" + UserName;

                // Set the LDAP path 
                string LDAPPath = _configuration["ldapConfiguration:ADPath"];

                // Create a DirectoryEntry to connect to the LDAP directory
                // Wrap in Task.Run to make this CPU-bound operation asynchronous
                bool isValid = await Task.Run(() =>
                {
                    DirectoryEntry entry = new DirectoryEntry(LDAPPath, UserName, Password);

                    if (string.IsNullOrEmpty(entry.Name))
                    {
                        return false; // Return false if no valid user name is found
                    }

                    return true; // Return true if valid
                });

                return isValid;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<ServiceResponse> PendingCountService(FilterModel filter)
        {
            try
            {
                var PendingCounts = await _loginRepo.PendingCountRepo(filter.EmpNo, filter.RoleId);

                return new ServiceResponse
                {
                    isError = false,
                    result = PendingCounts,
                    Message = "Pending Counts retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Pending",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        //************************************NORMAL LOGIN SERVICE FOR LOCAL/STAGING************************************

        public async Task<ServiceResponse> Login_Service(LoginModel loginModel)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(loginModel.EmpNo) ||
                    string.IsNullOrWhiteSpace(loginModel.Password))
                {
                    return new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Message = "Username and password are required"
                    };
                }

                // Additional validation for username format
                if (!_utility.IsValidUsername(loginModel.EmpNo))
                {
                    return new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Message = "Invalid username format"
                    };
                }

                TokenHelperModel tokenHelperModel = new TokenHelperModel();
                UserViewModel userViewModel = new UserViewModel();
                var result = await _loginRepo.LoginRepo(loginModel);
                response.Status = result.Status;
                response.Code = result.Code;
                response.Message = result.Message;

                if (result.Code == "200")
                {
                    tokenHelperModel.UserName = result.Name;
                    tokenHelperModel.EmpNo = result.EmpOrDNo;
                    tokenHelperModel.UserId = result.UserId;
                    tokenHelperModel.RoleId = result.RoleId;
                    tokenHelperModel.Role = result.Role;

                    // Generate JWT token and set cookie
                    string token = _jwtTokenGenerator.GenerateJsonWebToken(tokenHelperModel, _configuration);

                    // Don't include the token in the response model
                    userViewModel.UserName = result.Name;
                    userViewModel.EmpNo = result.EmpOrDNo;
                    userViewModel.Role = result.Role;
                    userViewModel.RoleId = result.RoleId;
                    userViewModel.Token = token;

                    response.result = userViewModel;
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Code = "400";
                response.isError = true;
                response.Message = "System can not find the combination of this Username and password, please try again";
            }
            return response;
        }

        //************************************LDAP LOGIN SERVICE FOR PRODUCTION************************************

        public async Task<ServiceResponse> LDAPLoginService(LoginModel loginModel)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                // Input validation
                if (string.IsNullOrWhiteSpace(loginModel.EmpNo) ||
                    string.IsNullOrWhiteSpace(loginModel.Password))
                {
                    return new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Message = "Username and password are required"
                    };
                }
                // Additional validation for username format
                if (!_utility.IsValidUsername(loginModel.EmpNo))
                {
                    return new ServiceResponse
                    {
                        Status = "Failure",
                        Code = "400",
                        Message = "Invalid username format"
                    };
                }

                var Ldap = await isLDAPAuthAsync(loginModel.EmpNo, loginModel.Password);

                if (Ldap == true)
                {
                    TokenHelperModel tokenHelperModel = new TokenHelperModel();
                    UserViewModel userViewModel = new UserViewModel();
                    var result = await _loginRepo.LDAPLoginRepo(loginModel, true);

                    response.Status = result.Status;
                    response.Code = result.Code;
                    response.Message = result.Message;

                    if (result.Code == "200")
                    {
                        tokenHelperModel.UserName = result.Name;
                        tokenHelperModel.EmpNo = result.EmpOrDNo;
                        tokenHelperModel.UserId = result.UserId;
                        tokenHelperModel.RoleId = result.RoleId;
                        tokenHelperModel.Role = result.Role;

                        // Generate JWT token and set cookie
                        string token = _jwtTokenGenerator.GenerateJsonWebToken(tokenHelperModel, _configuration);

                        // Don't include the token in the response model
                        userViewModel.UserName = result.Name;
                        userViewModel.EmpNo = result.EmpOrDNo;
                        userViewModel.Role = result.Role;
                        userViewModel.Token = token;

                        response.result = userViewModel;
                    }
                }
                else
                {
                    response.Status = "Failure";
                    response.Code = "401";
                    response.Message = "Unauthorized Access";
                }

            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Code = "400";
                response.isError = true;
                response.Message = "System can not find the combination of this Username and password, please try again";
            }
            return response;
        }

        public async Task<ServiceResponse> LogOutService(string empNo)
        {
            ServiceResponse response = new ServiceResponse();
            try
            {
                var logoutResult = await _loginRepo.LogoutRepo(empNo);

                // Map repository result to service response
                response.Status = logoutResult.Status;
                response.Code = logoutResult.Code;
                response.Message = logoutResult.Message;
            }
            catch (Exception ex)
            {
                // General exception handling for service-level failures
                response.Status = "Failure";
                response.Code = "500";
                response.isError = true;
                response.Message = "Failed to logout: " + ex.Message;
            }
            return response;
        }

        public async Task<bool> UpdateLoginHeartbeatService(string empNo)
        {
            return await _loginRepo.UpdateLoginHeartBeatRepo(empNo);
        }

        public async Task<bool> UpdateRegularHeartbeatService(string empNo)
        {
            return await _loginRepo.UpdateRegularHeartBeatRepo(empNo);
        }
    }
}