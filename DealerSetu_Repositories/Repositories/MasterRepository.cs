using DealerSetu_Repositories.IRepositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using DealerSetu.Repository.Common;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Repositories.Repositories
{
    public class MasterRepository : IMasterRepository
    {
        private readonly string _connectionString;
        private readonly Utility _utility;
        private readonly int _commandTimeout = 30; // Default command timeout in seconds

        public MasterRepository(IConfiguration configuration, Utility utility)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities") ??
                throw new InvalidOperationException("Connection string 'dbDealerSetuEntities' not found in configuration.");

            // Optional: Get command timeout from configuration or use default
            if (int.TryParse(configuration["DbCommandTimeout"], out int timeout) && timeout > 0)
            {
                _commandTimeout = timeout;
            }
        }

        public async Task<EmployeeListResult> EmployeeMasterRepo(string keyword, string role, int pageIndex, int pageSize)
        {
            using var sqlConnection = new SqlConnection(_connectionString);
            await sqlConnection.OpenAsync();

            try
            {
                // Convert role to integer if provided, null otherwise
                int? roleIdParam = null;
                if (!string.IsNullOrEmpty(role) && int.TryParse(role, out int roleId))
                {
                    roleIdParam = roleId;
                }

                var parameters = new DynamicParameters();
                parameters.Add("@Keyword", string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(), DbType.String);
                parameters.Add("@RoleId", roleIdParam, DbType.Int32);
                parameters.Add("@PageIndex", pageIndex, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);

                using var multi = await sqlConnection.QueryMultipleAsync(
                    "sp_MASTER_GetEmployeeList",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                var totalCount = await multi.ReadFirstAsync<int>();
                var employees = (await multi.ReadAsync<UserModel>()).ToList();

                return new EmployeeListResult
                {
                    Employees = employees,
                    TotalCount = totalCount
                };
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error occurred while retrieving employees: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving employees: {ex.Message}", ex);
            }
        }

        public async Task<List<RoleModel>> RolesDropdownRepo()
        {
            using var sqlConnection = new SqlConnection(_connectionString);
            await sqlConnection.OpenAsync();

            try
            {
                var roles = await sqlConnection.QueryAsync<RoleModel>(
                    "sp_MASTER_GetRoles",
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                return roles.ToList();
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error occurred while retrieving roles: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving roles: {ex.Message}", ex);
            }
        }

        public async Task<AdditionResponse<string>> AddEmployeeRepo(EmployeeModel emp)
        {
            var response = new AdditionResponse<string>();

            // Validate employee data
            if (emp == null)
            {
                response.IsError = true;
                response.Message = "Employee model cannot be null.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(emp.EmpNo))
            {
                response.IsError = true;
                response.Message = "Employee number cannot be empty.";
                return response;
            }

            // Email validation
            if (!string.IsNullOrEmpty(emp.Email) && !_utility.IsValidEmail(emp.Email))
            {
                response.IsError = true;
                response.Object = $"{emp.EmpNo} (Email: {emp.Email})";
                response.Message = "Invalid email address.";
                return response;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", emp.EmpNo?.Trim(), DbType.String);
                parameters.Add("@Name", emp.Name, DbType.String);
                parameters.Add("@Email", !string.IsNullOrEmpty(emp.Email) ? emp.Email.Trim()
                                        : $"{emp.EmpNo?.Trim()}@mahindra.com", DbType.String);
                parameters.Add("@RoleId", emp.RoleId, DbType.Int32);
                parameters.Add("@Password", emp.Password ?? emp.EmpNo?.Trim(), DbType.String);
                parameters.Add("@State", emp.State, DbType.String);

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_MASTER_InsertEmployeeData",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                if (result != null)
                {
                    response.IsError = result.IsError;
                    response.Message = result.Message;
                    response.Object = emp.EmpNo;
                }
                else
                {
                    response.IsError = false;
                    response.Message = "Employee saved successfully.";
                    response.Object = emp.EmpNo;
                }
            }
            catch (SqlException ex)
            {
                response.IsError = true;
                response.Message = $"Database error while saving employee: {ex.Message}";
                response.Object = emp.EmpNo;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"An error occurred while saving the employee: {ex.Message}";
                response.Object = emp.EmpNo;
            }

            return response;
        }

        public async Task<string> GetBlobUrlByFileNameAsync(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException("Filename cannot be null or empty.", nameof(fileName));
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            try
            {
                var blobUrl = await connection.QueryFirstOrDefaultAsync<string>(
                    "sp_MASTER_GetBlobUrlByFileName",
                    new { FileName = fileName.Trim() },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout
                );

                return blobUrl;
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error occurred while retrieving blob URL: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving blob URL: {ex.Message}", ex);
            }
        }

        public async Task<AdditionResponse<string>> AddDealerRepo(DealerModel dealer)
        {
            var response = new AdditionResponse<string>();

            // Validate dealer data
            if (dealer == null)
            {
                response.IsError = true;
                response.Message = "Dealer model cannot be null.";
                return response;
            }

            if (string.IsNullOrWhiteSpace(dealer.DealerNo))
            {
                response.IsError = true;
                response.Message = "Dealer number cannot be empty.";
                return response;
            }

            // Email validation
            if (!string.IsNullOrEmpty(dealer.Email) && !_utility.IsValidEmail(dealer.Email))
            {
                response.IsError = true;
                response.Object = $"{dealer.DealerNo} (Email: {dealer.Email})";
                response.Message = "Invalid email address.";
                return response;
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Prepare parameters for stored procedure
                var parameters = CreateDealerParameters(dealer);

                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "sp_MASTER_InsertDealerData",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout
                );

                if (result != null)
                {
                    response.IsError = result.IsError;
                    response.Message = result.Message;
                    response.Object = dealer.DealerNo;
                }
                else
                {
                    response.IsError = true;
                    response.Message = "No response from database procedure.";
                    response.Object = dealer.DealerNo;
                }
            }
            catch (SqlException ex)
            {
                response.IsError = true;
                response.Message = $"Database error while saving dealer data: {ex.Message}";
                response.Object = dealer.DealerNo;
            }
            catch (Exception ex)
            {
                response.IsError = true;
                response.Message = $"An error occurred while saving the dealer data: {ex.Message}";
                response.Object = ex.Message;
            }

            return response;
        }

        public async Task<DealerListResult> DealerMasterRepo(string keyword, int pageIndex, int pageSize)
        {
            using var sqlConnection = new SqlConnection(_connectionString);
            await sqlConnection.OpenAsync();

            try
            {
                var parameters = new DynamicParameters();
                // Handle null, empty, or "string" as a null value for the keyword parameter
                parameters.Add("@Keyword",
                    (string.IsNullOrWhiteSpace(keyword) || keyword == "string") ? null : keyword.Trim(),
                    DbType.String);
                parameters.Add("@PageIndex", pageIndex, DbType.Int32);
                parameters.Add("@PageSize", pageSize, DbType.Int32);

                using var multi = await sqlConnection.QueryMultipleAsync(
                    "sp_MASTER_GetDealerList",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                var totalCount = await multi.ReadFirstAsync<int>();
                var dealers = (await multi.ReadAsync<DealerModel>()).ToList();

                return new DealerListResult
                {
                    Dealers = dealers,
                    TotalCount = totalCount
                };
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error occurred while retrieving dealers: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while retrieving dealers: {ex.Message}", ex);
            }
        }

        public async Task<(int RowsAffected, string Message)> UpdateEmployeeDetailsRepo(int? userId, string name, string email)
        {
            try
            {
                // Validate required parameters
                if (!userId.HasValue || userId.Value <= 0)
                {
                    return (0, "Invalid User ID.");
                }

                // Email validation if provided and not "string" placeholder
                if (!string.IsNullOrEmpty(email) && email != "string" && !_utility.IsValidEmail(email))
                {
                    return (0, "Invalid email address format.");
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId, DbType.Int32);
                parameters.Add("@Name", string.IsNullOrWhiteSpace(name) || name == "string" ? null : name.Trim(), DbType.String);
                parameters.Add("@Email", string.IsNullOrWhiteSpace(email) || email == "string" ? null : email.Trim(), DbType.String);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
                parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                await connection.ExecuteAsync(
                    "sp_MASTER_UpdateEmployeeDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                int rowsAffected = parameters.Get<int>("@ReturnValue");
                string message = parameters.Get<string>("@Message") ?? "Update completed.";

                return (rowsAffected, message);
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error while updating employee details: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while updating employee details: {ex.Message}", ex);
            }
        }

        public async Task<string> DeleteUserRepo(int userId)
        {
            if (userId <= 0)
            {
                return "Invalid user ID specified.";
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId, DbType.Int32);

                var message = await connection.QuerySingleAsync<string>(
                    "sp_MASTER_DeleteUser",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                return message;
            }
            catch (SqlException ex)
            {
                return $"Database error occurred: {ex.Message}";
            }
            catch (Exception)
            {
                return "An unexpected error occurred while deleting the user.";
            }
        }

        public async Task<(int RowsAffected, string Message)> UpdateDealerDetailsRepo(int? userId, string empName, string location, string district, string zone, string state)
        {
            if (!userId.HasValue || userId.Value <= 0)
            {
                return (0, "Invalid User ID.");
            }

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@UserId", userId, DbType.Int32);
                parameters.Add("@EmpName", NormalizeInput(empName), DbType.String);
                parameters.Add("@Location", NormalizeInput(location), DbType.String);
                parameters.Add("@District", NormalizeInput(district), DbType.String);
                parameters.Add("@Zone", NormalizeInput(zone), DbType.String);
                parameters.Add("@State", NormalizeInput(state), DbType.String);
                parameters.Add("@Message", dbType: DbType.String, direction: ParameterDirection.Output, size: 255);
                parameters.Add("@ReturnValue", dbType: DbType.Int32, direction: ParameterDirection.ReturnValue);

                await connection.ExecuteAsync(
                    "sp_MASTER_UpdateDealerDetails",
                    parameters,
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _commandTimeout);

                // Get return values
                int rowsAffected = parameters.Get<int>("@ReturnValue");
                string message = parameters.Get<string>("@Message") ?? "Update completed.";

                return (rowsAffected, message);
            }
            catch (SqlException ex)
            {
                throw new Exception($"Database error while updating dealer details: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error occurred while updating dealer details: {ex.Message}", ex);
            }
        }

        #region Private Helper Methods

        private DynamicParameters CreateDealerParameters(DealerModel dealer)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@DealerNo", dealer.DealerNo?.Trim(), DbType.String);
            parameters.Add("@DealershipName", dealer.DealershipName, DbType.String);
            parameters.Add("@Email", !string.IsNullOrEmpty(dealer.Email) ? dealer.Email.Trim()
                                    : $"{dealer.DealerNo?.Trim()}@company.com", DbType.String);
            parameters.Add("@District", dealer.District, DbType.String);
            parameters.Add("@Location", dealer.Location, DbType.String);
            parameters.Add("@Zone", dealer.Zone, DbType.String);
            parameters.Add("@State", dealer.State, DbType.String);
            parameters.Add("@DateOfAppointment", dealer.DateOfAppointment, DbType.String);
            parameters.Add("@DealershipAge", dealer.DealershipAge, DbType.String);
            parameters.Add("@Industry", dealer.Industry, DbType.String);
            parameters.Add("@TRVolPlan", dealer.TRVolPlan, DbType.String);
            parameters.Add("@PlanVol", dealer.PlanVol, DbType.String);
            parameters.Add("@OwnFund", dealer.OwnFund, DbType.String);
            parameters.Add("@BGFund", dealer.BGFund, DbType.String);
            parameters.Add("@SalesManpower", dealer.SalesManpower, DbType.String);
            parameters.Add("@ServiceManpower", dealer.ServiceManpower, DbType.String);
            parameters.Add("@AdminManpower", dealer.AdminManpower, DbType.String);
            parameters.Add("@ShowroomSize", dealer.ShowroomSize, DbType.String);
            parameters.Add("@WorkshopSize", dealer.WorkshopSize, DbType.String);
            parameters.Add("@SH", dealer.SH?.Trim(), DbType.String);
            parameters.Add("@TM", dealer.TM?.Trim(), DbType.String);
            parameters.Add("@AM", dealer.AM?.Trim(), DbType.String);
            parameters.Add("@SCM", dealer.SCM?.Trim(), DbType.String);
            parameters.Add("@CCM", dealer.CCM?.Trim(), DbType.String);
            parameters.Add("@CM", dealer.CM?.Trim(), DbType.String);

            return parameters;
        }

        private string NormalizeInput(string input)
        {
            // Handle null, empty, or "string" placeholder values
            return string.IsNullOrWhiteSpace(input) || input == "string" ? null : input.Trim();
        }

        #endregion
    }
}