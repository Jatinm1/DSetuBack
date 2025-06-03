using DealerSetu_Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Repository for managing user authentication and session operations
    /// </summary>
    public class LoginRepository : ILoginRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the LoginRepository
        /// </summary>
        /// <param name="configuration">Configuration containing connection string</param>
        public LoginRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentException("Connection string 'dbDealerSetuEntities' not found");
        }

        /// <summary>
        /// Authenticates user with employee number and password
        /// </summary>
        /// <param name="loginModel">Login credentials</param>
        /// <returns>Authentication result or error response</returns>
        public async Task<dynamic> LoginRepo(LoginModel loginModel)
        {
            if (loginModel == null)
                return CreateErrorResponse("Invalid login data");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", loginModel.EmpNo, DbType.String, size: 200);
                parameters.Add("@Password", loginModel.Password, DbType.String, size: 200);

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_LOGIN_LoginUser",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result ?? CreateErrorResponse("Authentication failed");
            }
            catch (SqlException)
            {
                return CreateErrorResponse("Database connection failed");
            }
            catch (Exception)
            {
                return CreateErrorResponse("Authentication service unavailable");
            }
        }

        /// <summary>
        /// Authenticates user via LDAP with employee number only
        /// </summary>
        /// <param name="loginModel">Login credentials</param>
        /// <param name="ldapValidated">LDAP validation status</param>
        /// <returns>Authentication result or error response</returns>
        public async Task<dynamic> LDAPLoginRepo(LoginModel loginModel, bool ldapValidated)
        {
            if (loginModel == null || !ldapValidated)
                return CreateErrorResponse("Invalid LDAP authentication");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", loginModel.EmpNo, DbType.String, size: 200);

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_LOGIN_LoginUserLDAP",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result ?? CreateErrorResponse("LDAP authentication failed");
            }
            catch (SqlException)
            {
                return CreateErrorResponse("Database connection failed");
            }
            catch (Exception)
            {
                return CreateErrorResponse("LDAP service unavailable");
            }
        }

        /// <summary>
        /// Retrieves pending work counts for user dashboard
        /// </summary>
        /// <param name="empNo">Employee number</param>
        /// <param name="roleId">User role identifier</param>
        /// <returns>List of pending counts by category</returns>
        public async Task<List<PendingCountModel>> PendingCountRepo(string empNo, string roleId)
        {
            if (string.IsNullOrWhiteSpace(empNo) || string.IsNullOrWhiteSpace(roleId))
                return new List<PendingCountModel>();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", empNo, DbType.String);
                parameters.Add("@RoleId", roleId, DbType.String);

                var results = await connection.QueryAsync<PendingCountModel>(
                    "sp_HOME_GetAllPendingCounts",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return results?.ToList() ?? new List<PendingCountModel>();
            }
            catch (Exception)
            {
                return new List<PendingCountModel>();
            }
        }

        /// <summary>
        /// Logs out user and cleans up session data
        /// </summary>
        /// <param name="empNo">Employee number</param>
        /// <returns>Service response indicating success or failure</returns>
        public async Task<dynamic> LogoutRepo(string empNo)
        {
            if (string.IsNullOrWhiteSpace(empNo))
                return CreateErrorResponse("Invalid employee number", "400");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@empNo", empNo);

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_LOGIN_LogoutUser",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return new ServiceResponse
                {
                    Status = result?.Code == "200" ? "Success" : "Failure",
                    Code = result?.Code ?? "400",
                    Message = result?.Message ?? "Logout failed",
                    isError = result?.Code != "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    Status = "Failure",
                    Code = "500",
                    Message = "Logout operation failed",
                    isError = true
                };
            }
        }

        /// <summary>
        /// Updates user login heartbeat for session tracking
        /// </summary>
        /// <param name="empNo">Employee number</param>
        /// <returns>True if heartbeat updated successfully</returns>
        public async Task<bool> UpdateLoginHeartBeatRepo(string empNo)
        {
            if (string.IsNullOrWhiteSpace(empNo))
                return false;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.ExecuteAsync(
                    "sp_LOGIN_UpdateLoginHeartBeat",
                    new { EmpNo = empNo },
                    commandType: CommandType.StoredProcedure);

                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Updates regular heartbeat for active session monitoring
        /// </summary>
        /// <param name="empNo">Employee number</param>
        /// <returns>True if heartbeat updated successfully</returns>
        public async Task<bool> UpdateRegularHeartBeatRepo(string empNo)
        {
            if (string.IsNullOrWhiteSpace(empNo))
                return false;

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var result = await connection.QueryFirstOrDefaultAsync<int>(
                    "sp_LOGIN_UpdateRegularHeartBeat",
                    new { EmpNo = empNo },
                    commandType: CommandType.StoredProcedure);

                return result == 1;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Creates standardized error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code (default: 500)</param>
        /// <returns>ServiceResponse with error details</returns>
        private static ServiceResponse CreateErrorResponse(string message, string code = "500")
        {
            return new ServiceResponse
            {
                Message = message,
                Status = "Failure",
                Code = code,
                isError = true
            };
        }
    }
}