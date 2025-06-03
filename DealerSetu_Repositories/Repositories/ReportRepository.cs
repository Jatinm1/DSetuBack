using DealerSetu_Repositories.IRepositories;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using DealerSetu.Repository.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Repositories.Repositories
{
    /// <summary>
    /// Production-grade repository for handling report data operations with optimized performance and error handling
    /// </summary>
    public sealed class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;
        private readonly Utility _utility;

        /// <summary>
        /// Initializes a new instance of the ReportRepository with required dependencies
        /// </summary>
        /// <param name="configuration">Application configuration containing connection strings</param>
        /// <param name="utility">Utility service for common operations</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
        /// <exception cref="InvalidOperationException">Thrown when connection string is not found</exception>
        public ReportRepository(IConfiguration configuration, Utility utility)
        {
            _connectionString = configuration?.GetConnectionString("dbDealerSetuEntities")
                ?? throw new InvalidOperationException("Connection string 'dbDealerSetuEntities' not found.");
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
        }

        /// <summary>
        /// Retrieves paginated report data for request sections based on specified filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria containing date range and user information</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>Tuple containing list of reports and total record count</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pagination parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
        public async Task<(List<ReportModel> Reports, int TotalCount)> RequestSectionReportRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            ValidateInputs(filter, pageIndex, pageSize);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = CreateBasicParameters(filter, pageIndex, pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_LogForRequests",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var reports = (await multi.ReadAsync<ReportModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (reports, totalCount);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving report data.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing report request.", ex);
            }
        }

        /// <summary>
        /// Retrieves count summary of rejected requests including claim plans and demo tractors for specified date range
        /// </summary>
        /// <param name="filter">Filter criteria containing date range</param>
        /// <returns>Summary model containing counts of rejected claim plans and demo tractors</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
        public async Task<DemoTractor> RejectedRequestReportRepo(FilterModel filter)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var dateParameters = CreateDateParameters(filter);

                // Execute both stored procedures concurrently for better performance
                var claimPlansTask = connection.QueryAsync<Rejectrequest>(
                    "sp_REPORT_RejectedClaimPlans",
                    dateParameters,
                    commandType: CommandType.StoredProcedure);

                var demoTractorsTask = connection.QueryAsync<DemoTractorReject>(
                    "sp_REPORT_RejectedDemoTractors",
                    dateParameters,
                    commandType: CommandType.StoredProcedure);

                await Task.WhenAll(claimPlansTask, demoTractorsTask);

                return new DemoTractor
                {
                    newdealercount = (await claimPlansTask).Count(),
                    democount = (await demoTractorsTask).Count()
                };
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving rejected request data.", ex);
            }
            catch (Exception ex)
            {
                _utility.ExcepLog(ex);
                throw new InvalidOperationException("Unexpected error occurred while processing rejected request report.", ex);
            }
        }

        /// <summary>
        /// Retrieves state-wise dealer information and activity summary for specified fiscal year
        /// </summary>
        /// <param name="fy">Fiscal year for which to retrieve dealer state data</param>
        /// <returns>List of state-wise dealer activity models</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when fiscal year is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
        public async Task<List<DealerstateModel>> NewDealerStatewiseReportRepo(int fy)
        {
            if (fy <= 0) throw new ArgumentOutOfRangeException(nameof(fy), "Fiscal year must be a positive integer.");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@fy", fy);

                var result = await connection.QueryAsync<DealerstateModel>(
                    "sp_REPORT_StatewiseActivity",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return result.ToList();
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error occurred while retrieving state-wise data for fiscal year {fy}.", ex);
            }
            catch (Exception ex)
            {
                _utility.ExcepLog(ex);
                throw new InvalidOperationException($"Unexpected error occurred while processing state-wise report for fiscal year {fy}.", ex);
            }
        }

        /// <summary>
        /// Retrieves paginated demo tractor request data based on specified filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria containing date range and request number</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>Tuple containing list of demo requests and total record count</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pagination parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
        public async Task<(List<DemoListModel> DemoRequests, int TotalCount)> DemoTractorReportRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            ValidateInputs(filter, pageIndex, pageSize);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = CreateRequestParameters(filter, pageIndex, pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_DemoTractorLog",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var demoRequests = (await multi.ReadAsync<DemoListModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (demoRequests, totalCount);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving demo tractor data.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing demo tractor report.", ex);
            }
        }

        /// <summary>
        /// Retrieves paginated new dealer activity data with optional filtering by head office pending status
        /// </summary>
        /// <param name="filter">Filter criteria containing date range and request details</param>
        /// <param name="pendingByHO">Optional filter for head office pending status</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>Tuple containing list of new dealer activities and total record count</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pagination parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
        public async Task<(List<NewDealerActivity> NewDealerActivities, int TotalCount)> NewDealerActivityReportRepo(
            FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            ValidateInputs(filter, pageIndex, pageSize);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = CreateNewDealerActivityParameters(filter, pendingByHO, pageIndex, pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_NewDealerActivityLog",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var activities = (await multi.ReadAsync<NewDealerActivity>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (activities, totalCount);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving new dealer activity data.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing new dealer activity report.", ex);
            }
        }

        /// <summary>
        /// Retrieves paginated new dealer claim activity data for dealer-specific claim requests
        /// </summary>
        /// <param name="filter">Filter criteria containing date range and request details</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>Tuple containing list of new dealer claim activities and total record count</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pagination parameters are invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when database operation fails</exception>
        public async Task<(List<NewDealerActivityClaim> NewDealerClaimActivities, int TotalCount)> NewDealerClaimReportRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            ValidateInputs(filter, pageIndex, pageSize);

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = CreateClaimParameters(filter, pageIndex, pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_ClaimListNewDealer",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var claimActivities = (await multi.ReadAsync<NewDealerActivityClaim>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (claimActivities, totalCount);
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving new dealer claim data.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing new dealer claim report.", ex);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates common input parameters for pagination and filter requirements
        /// </summary>
        private static void ValidateInputs(FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (pageIndex < 0) throw new ArgumentOutOfRangeException(nameof(pageIndex), "Page index must be non-negative.");
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "Page size must be positive.");
        }

        /// <summary>
        /// Creates standardized parameters for basic filtering, pagination, and user authentication
        /// </summary>
        private static DynamicParameters CreateBasicParameters(FilterModel filter, int pageIndex, int pageSize)
        {
            var parameters = new DynamicParameters();

            AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@EmpNo", filter.EmpNo);
            AddParameterIfNotNull(parameters, "@RoleId", filter.RoleId);

            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            return parameters;
        }

        /// <summary>
        /// Creates parameters specifically for date range filtering operations
        /// </summary>
        private static DynamicParameters CreateDateParameters(FilterModel filter)
        {
            var parameters = new DynamicParameters();

            AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));

            return parameters;
        }

        /// <summary>
        /// Creates parameters for request-based filtering with pagination support
        /// </summary>
        private static DynamicParameters CreateRequestParameters(FilterModel filter, int pageIndex, int pageSize)
        {
            var parameters = new DynamicParameters();

            AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@RequestNo", filter.RequestNo);

            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            return parameters;
        }

        /// <summary>
        /// Creates parameters for new dealer activity queries with head office pending status
        /// </summary>
        private static DynamicParameters CreateNewDealerActivityParameters(
            FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            var parameters = new DynamicParameters();

            AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@RequestNo", filter.RequestNo);
            AddParameterIfNotNull(parameters, "@PendingByHO", pendingByHO);

            parameters.Add("@Export", filter.Export);
            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            return parameters;
        }

        /// <summary>
        /// Creates parameters for dealer claim queries with fixed request type identifier
        /// </summary>
        private static DynamicParameters CreateClaimParameters(FilterModel filter, int pageIndex, int pageSize)
        {
            var parameters = new DynamicParameters();

            parameters.Add("@RequestTypeId", 2); // Fixed request type for dealer claims

            AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@RequestNo", filter.RequestNo);

            parameters.Add("@PageIndex", pageIndex);
            parameters.Add("@PageSize", pageSize);

            return parameters;
        }

        /// <summary>
        /// Conditionally adds parameters to avoid null value issues in stored procedure calls
        /// </summary>
        private static void AddParameterIfNotNull(DynamicParameters parameters, string name, object value)
        {
            if (value != null)
            {
                parameters.Add(name, value);
            }
        }

        #endregion
    }
}