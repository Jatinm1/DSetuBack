using DealerSetu_Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using static Dapper.SqlMapper;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Repositories.Repositories
{
    public class LoginRepository : ILoginRepository
    {
        private readonly IConfiguration _configuration;
        private static string _connString;

        public LoginRepository(IConfiguration configuration)
        {
            _configuration = configuration;
            _connString = configuration.GetConnectionString("dbDealerSetuEntities");
        }

        public async Task<dynamic> LoginRepo(LoginModel loginModel)
        {
            using (var sqlConnection = new SqlConnection(_connString))
            {
                try
                {
                    await sqlConnection.OpenAsync();

                    var parameter = new DynamicParameters();
                    parameter.Add("@EmpNo", loginModel.EmpNo, DbType.String, size: 200);
                    parameter.Add("@Password", loginModel.Password, DbType.String, size: 200);

                    return (await sqlConnection.QueryAsync<dynamic>(
                        "sp_LOGIN_LoginUser",
                        parameter,
                        commandType: CommandType.StoredProcedure)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    return new ServiceResponse
                    {
                        Message = "An error occurred",
                        Status = "Failure",
                        Code = "500",
                        isError = true,
                    };
                }
            }
        }

        public async Task<dynamic> LDAPLoginRepo(LoginModel loginModel, bool ldapValidated)
        {
            using (var sqlConnection = new SqlConnection(_connString))
            {
                try
                {
                    await sqlConnection.OpenAsync();

                    var parameter = new DynamicParameters();
                    parameter.Add("@EmpNo", loginModel.EmpNo, DbType.String, size: 200);

                    return (await sqlConnection.QueryAsync<dynamic>(
                        "sp_LOGIN_LoginUserLDAP",
                        parameter,
                        commandType: CommandType.StoredProcedure)).FirstOrDefault();
                }
                catch (Exception ex)
                {
                    return new ServiceResponse
                    {
                        Message = "An error occurred",
                        Status = "Failure",
                        Code = "500",
                        isError = true,
                    };
                }
            }
        }

        public async Task<List<PendingCountModel>> PendingCountRepo(string empNo, string roleId)
        {
            try
            {
                using (var connection = new SqlConnection(_connString))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@EmpNo", empNo, DbType.String);
                    parameters.Add("@RoleId", roleId, DbType.String);

                    // Use QueryAsync to get all rows
                    var results = await connection.QueryAsync<PendingCountModel>(
                        "sp_HOME_GetAllPendingCounts",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return results.ToList();
                }
            }
            catch (Exception ex)
            {
                // Consider logging the exception here
                return new List<PendingCountModel>();
            }
        }

        public async Task<dynamic> LogoutRepo(string empNo)
        {
            var response = new ServiceResponse();
            try
            {
                using (var sqlConnection = new SqlConnection(_connString))
                {
                    await sqlConnection.OpenAsync();

                    var parameter = new DynamicParameters();
                    parameter.Add("@empNo", empNo);

                    var result = await sqlConnection.QueryFirstOrDefaultAsync<dynamic>(
                        "sp_LOGIN_LogoutUser",
                        parameter,
                        commandType: CommandType.StoredProcedure
                    );

                    response.Status = result?.Code == "200" ? "Success" : "Failure";
                    response.Code = result?.Code ?? "400";
                    response.Message = result?.Message ?? "Logout failed";
                }
            }
            catch (Exception ex)
            {
                response.Status = "Failure";
                response.Code = "400";
                response.Message = "Logout failed: " + ex.Message;
            }
            return response;
        }

        public async Task<bool> UpdateLoginHeartBeatRepo(string empNo)
        {
            using (var sqlConnection = new SqlConnection(_connString))
            {
                await sqlConnection.OpenAsync();

                var result = await sqlConnection.ExecuteAsync(
                    "sp_LOGIN_UpdateLoginHeartBeat",
                    new { EmpNo = empNo },
                    commandType: CommandType.StoredProcedure);

                return result > 0;
            }
        }

        public async Task<bool> UpdateRegularHeartBeatRepo(string empNo)
        {
            using (var sqlConnection = new SqlConnection(_connString))
            {
                await sqlConnection.OpenAsync();

                var result = await sqlConnection.QueryFirstOrDefaultAsync<int>(
                    "sp_LOGIN_UpdateRegularHeartBeat",
                    new { EmpNo = empNo },
                    commandType: CommandType.StoredProcedure);

                return result == 1; // Return true if SP returns 1, else false
            }
        }
    }
}