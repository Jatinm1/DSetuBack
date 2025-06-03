using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.HelperModels;

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
        /// <returns>List of request type filter models</returns>
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
        /// <returns>List of HP category filter models</returns>
        public async Task<List<HPCategoryFilterModel>> HPCategoryFilterRepo()
        {
            using var connection = new SqlConnection(_connectionString);

            var hpCategories = await connection.QueryAsync<HPCategoryFilterModel>(
                "sp_REQUEST_GetHPCategories",
                commandType: CommandType.StoredProcedure);

            return hpCategories.ToList();
        }

        /// <summary>
        /// Submits a new request to the system
        /// </summary>
        /// <param name="requestTypeId">ID of the request type</param>
        /// <param name="message">Request message content</param>
        /// <param name="hpCategory">HP category ID (optional)</param>
        /// <param name="empNo">Employee number</param>
        /// <returns>Request submission result with generated details</returns>
        public async Task<RequestSubmissionResult> SubmitRequestAsync(string requestTypeId, string message, string hpCategory, string empNo)
        {
            if (string.IsNullOrWhiteSpace(requestTypeId) || !int.TryParse(requestTypeId, out var parsedRequestTypeId))
                throw new ArgumentException("Invalid request type ID", nameof(requestTypeId));

            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty", nameof(message));

            if (string.IsNullOrWhiteSpace(empNo))
                throw new ArgumentException("Employee number cannot be empty", nameof(empNo));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestTypeId", parsedRequestTypeId, DbType.Int32);
            parameters.Add("@Message", message, DbType.String);
            parameters.Add("@HpCategoryId", string.IsNullOrEmpty(hpCategory) ? (int?)null : int.Parse(hpCategory), DbType.Int32);
            parameters.Add("@EmpNo", empNo, DbType.String);

            // Output parameters
            parameters.Add("@RequestId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@RequestNo", dbType: DbType.String, size: 50, direction: ParameterDirection.Output);
            parameters.Add("@RequestTypeName", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

            await connection.QueryMultipleAsync(
                "sp_REQUEST_SubmitRequest",
                parameters,
                commandType: CommandType.StoredProcedure);

            return new RequestSubmissionResult
            {
                RequestId = parameters.Get<int>("@RequestId"),
                RequestNo = parameters.Get<string>("@RequestNo"),
                RequestTypeName = parameters.Get<string>("@RequestTypeName")
            };
        }

        /// <summary>
        /// Retrieves paginated list of requests based on filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria for requests</param>
        /// <param name="pageIndex">Page index for pagination (0-based)</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Tuple containing list of requests and total count</returns>
        public async Task<(List<RequestModel> requests, int TotalCount)> RequestListRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (pageIndex < 0)
                throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be positive", nameof(pageSize));

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@RequestTypeId", string.IsNullOrEmpty(filter.RequestTypeId) ? null : int.Parse(filter.RequestTypeId));
            parameters.Add("@FromDate", (DateTime?)null);
            parameters.Add("@ToDate", (DateTime?)null);
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