using Dapper;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Repository for handling request-related database operations
    /// </summary>
    public class RequestRepository : IRequestRepository
    {
        private readonly string _connectionString;

        public RequestRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string cannot be null");
        }

        /// <summary>
        /// Retrieves all available request types for filtering
        /// </summary>
        public async Task<List<RequestTypeFilterModel>> RequestTypeFilterRepo()
        {
            using var connection = new SqlConnection(_connectionString);
            var requestTypes = await connection.QueryAsync<RequestTypeFilterModel>(
                "sp_REQUEST_GetRequestTypes",
                commandType: CommandType.StoredProcedure);

            return requestTypes.ToList();
        }

        /// <summary>
        /// Retrieves all available HP categories for filtering
        /// </summary>
        public async Task<List<HPCategoryFilterModel>> HPCategoryFilterRepo()
        {
            using var connection = new SqlConnection(_connectionString);
            var hpCategories = await connection.QueryAsync<HPCategoryFilterModel>(
                "sp_REQUEST_GetHPCategories",
                commandType: CommandType.StoredProcedure);

            return hpCategories.ToList();
        }

        /// <summary>
        /// Submits a new request to the database
        /// </summary>
        public async Task<RequestSubmissionResult> SubmitRequestAsync(RequestSubmissionModel request, string empNo, string roleId)
        {
            if (!int.TryParse(request.RequestTypeId, out var parsedRequestTypeId))
                throw new ArgumentException("Invalid request type ID", nameof(request.RequestTypeId));

            if (!int.TryParse(roleId, out var parsedRoleId))
                throw new ArgumentException("Invalid RoleId", nameof(roleId));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestTypeId", parsedRequestTypeId, DbType.Int32);
            parameters.Add("@Message", request.Message, DbType.String);
            parameters.Add("@HpCategoryId", string.IsNullOrEmpty(request.HpCategory) ? null : int.Parse(request.HpCategory), DbType.Int32);
            parameters.Add("@EmpNo", empNo, DbType.String);
            parameters.Add("@RoleId", parsedRoleId, DbType.Int32);

            // Output parameters
            parameters.Add("@RequestId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@RequestNo", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
            parameters.Add("@RequestTypeName", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

            await connection.QueryMultipleAsync(
                "sp_REQUEST_SubmitRequest",
                parameters,
                commandType: CommandType.StoredProcedure);

            var requestId = parameters.Get<int>("@RequestId");
            if (requestId == -2)
                throw new ArgumentException("Unauthorized Role for Request Submission");

            return new RequestSubmissionResult
            {
                RequestId = requestId,
                RequestNo = parameters.Get<string>("@RequestNo"),
                RequestTypeName = parameters.Get<string>("@RequestTypeName")
            };
        }

        /// <summary>
        /// Retrieves paginated list of requests based on filter criteria
        /// </summary>
        public async Task<(List<RequestModel> requests, int TotalCount)> RequestListRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestTypeId", string.IsNullOrEmpty(filter.RequestTypeId) ? null : int.Parse(filter.RequestTypeId));
            parameters.Add("@FromDate", filter.From);
            parameters.Add("@ToDate", filter.To);
            parameters.Add("@RequestNo", string.IsNullOrEmpty(filter.RequestNo) ? null : filter.RequestNo);
            parameters.Add("@EmpNo", filter.EmpNo);
            parameters.Add("@RoleId", filter.RoleId);
            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            using var multi = await connection.QueryMultipleAsync(
                "sp_REQUEST_GetRequestList",
                parameters,
                commandType: CommandType.StoredProcedure);

            var requests = (await multi.ReadAsync<RequestModel>()).ToList();
            var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

            return (requests, totalCount);
        }
    }
}