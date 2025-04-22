using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using DealerSetu_Repositories.IRepositories;
using Microsoft.IdentityModel.Tokens;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Repositories.Repositories
{
    public class RequestRepository : IRequestRepository
    {
        private readonly string _connectionString;

        public RequestRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities");
        }

        public async Task<List<RequestTypeFilterModel>> RequestTypeFilterRepo()
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                try
                {
                    var requestTypes = sqlConnection.Query<RequestTypeFilterModel>(
                        "sp_REQUEST_GetRequestTypes",
                        commandType: CommandType.StoredProcedure).ToList();

                    return requestTypes;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<List<HPCategoryFilterModel>> HPCategoryFilterRepo()
        {
            using (var sqlConnection = new SqlConnection(_connectionString))
            {
                try
                {
                    var HPCategories = sqlConnection.Query<HPCategoryFilterModel>(
                        "sp_REQUEST_GetHPCategories",
                        commandType: CommandType.StoredProcedure).ToList();

                    return HPCategories;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public async Task<RequestSubmissionResult> SubmitRequestAsync(string requestTypeId, string message, string hpCategory, string empNo)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Define parameters for the stored procedure
                var parameters = new DynamicParameters();
                parameters.Add("@RequestTypeId", int.Parse(requestTypeId), DbType.Int32);
                parameters.Add("@Message", message, DbType.String);
                parameters.Add("@HpCategoryId", string.IsNullOrEmpty(hpCategory) ? (int?)null : int.Parse(hpCategory), DbType.Int32);
                parameters.Add("@EmpNo", empNo, DbType.String);

                // Output parameters
                parameters.Add("@RequestId", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@RequestNo", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
                parameters.Add("@RequestTypeName", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

                // Execute the stored procedure
                var result = await connection.QueryMultipleAsync(
                    "sp_REQUEST_SubmitRequest",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                //// Retrieve the dealer info
                //var dealerInfo = await result.ReadFirstOrDefaultAsync<DealerInfo>();

                //// Retrieve HO emails
                //var hoEmails = await result.ReadAsync<EmailModel>();

                //// Retrieve CC emails
                //var ccEmails = await result.ReadAsync<EmailModel>();

                //// Dealer email
                //var dealerEmail = new EmailModel
                //{
                //    Email = dealerInfo?.Email,
                //    Name = dealerInfo?.Name
                //};

                return new RequestSubmissionResult
                {
                    RequestId = parameters.Get<int>("@RequestId"),
                    RequestNo = parameters.Get<string>("@RequestNo"),
                    RequestTypeName = parameters.Get<string>("@RequestTypeName"),
                    //DealerName = dealerInfo?.Name,
                    //DealerLocation = dealerInfo?.DealerLocation,
                    //DealerState = dealerInfo?.DealerState,
                    //EmpNo = empNo,
                    //HOEmails = hoEmails.ToList(),
                    //CCEmails = ccEmails.ToList(),
                    //DealerEmail = dealerEmail
                };
            }
        }

        public async Task<(List<RequestModel> requests, int TotalCount)> RequestListRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                using (var sqlConnection = new SqlConnection(_connectionString))
                {
                    await sqlConnection.OpenAsync();

                    var parameters = new DynamicParameters();

                    // Parse RequestTypeId to int if not empty
                    if (!string.IsNullOrEmpty(filter.RequestTypeId))
                    {
                        parameters.Add("@RequestTypeId", int.Parse(filter.RequestTypeId));
                    }
                    else
                    {
                        parameters.Add("@RequestTypeId", null);
                    }

                    // Parse dates if provided
                    DateTime? fromDate = null;
                    DateTime? toDate = null;

                    parameters.Add("@FromDate", fromDate);
                    parameters.Add("@ToDate", toDate);
                    parameters.Add("@RequestNo", string.IsNullOrEmpty(filter.RequestNo) ? null : filter.RequestNo);
                    parameters.Add("@EmpNo", filter.EmpNo);
                    parameters.Add("@RoleId", filter.RoleId);
                    parameters.Add("@PageIndex", pageIndex);
                    parameters.Add("@PageSize", pageSize);

                    using (var multi = sqlConnection.QueryMultiple("sp_REQUEST_GetRequestList", parameters, commandType: CommandType.StoredProcedure))
                    {
                        // First result set: demo requests data
                        var requests = multi.Read<RequestModel>().ToList();

                        // Second result set: total count
                        var totalCount = multi.Read<int>().FirstOrDefault();

                        return (requests, totalCount);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception
                throw;
            }
        }
    }
}
