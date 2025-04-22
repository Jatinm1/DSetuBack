using DealerSetu_Repositories.IRepositories;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using DealerSetu.Repository.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Repositories.Repositories
{
    public class ReportRepository : IReportRepository
    {
        private readonly string _connectionString;
        private readonly Utility _utility;
        private readonly ILogger<ReportRepository> _logger;

        public ReportRepository(IConfiguration configuration, Utility utility, ILogger<ReportRepository> logger)
        {
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            _connectionString = configuration.GetConnectionString("dbDealerSetuEntities")
                ?? throw new InvalidOperationException("Connection string not found.");
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a paginated list of reports based on filter criteria
        /// </summary>
        public async Task<(List<ReportModel> Reports, int TotalCount)> RequestSectionReportRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation($"Executing RequestSectionReportRepo with filter: FromDate={filter.From}, ToDate={filter.To}, PageIndex={pageIndex}, PageSize={pageSize}");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = CreateBasicParameters(filter, pageIndex, pageSize);

                using var multi = await connection.QueryMultipleAsync("sp_REPORT_LogForRequests", parameters, commandType: CommandType.StoredProcedure);

                var reports = (await multi.ReadAsync<ReportModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                _logger.LogInformation($"RequestSectionReportRepo completed. Retrieved {reports.Count} reports out of {totalCount} total records.");

                return (reports, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RequestSectionReportRepo");
                throw new Exception("Error while retrieving report list.", ex);
            }
        }

        /// <summary>
        /// Retrieves a list of rejected requests based on date range
        /// </summary>
        public async Task<DemoTractor> RejectedRequestReportRepo(FilterModel filter)
        {
            _logger.LogInformation($"Executing RejectedRequestReportRepo with filter: FromDate={filter.From}, ToDate={filter.To}");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Create date range parameters
                var dateParameters = CreateDateParameters(filter);

                // Fetch rejected claim plans
                var claimPlans = (await connection.QueryAsync<Rejectrequest>(
                    "sp_REPORT_RejectedClaimPlans",
                    dateParameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                // Fetch rejected demo tractors using the same parameters
                var demoTractors = (await connection.QueryAsync<DemoTractorReject>(
                    "sp_REPORT_RejectedDemoTractors",
                    dateParameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                // Create summary model with counts
                var countModel = new DemoTractor
                {
                    newdealercount = claimPlans.Count,
                    democount = demoTractors.Count
                };

                _logger.LogInformation($"RejectedRequestReportRepo completed. Found {claimPlans.Count} rejected claim plans and {demoTractors.Count} rejected demo tractors.");

                return countModel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectedRequestReportRepo");
                throw;
            }
        }

        /// <summary>
        /// Retrieves state-wise dealer information for a given fiscal year
        /// </summary>
        public async Task<List<DealerstateModel>> NewDealerStatewiseReportRepo(int fy)
        {
            _logger.LogInformation($"Executing NewDealerStatewiseReportRepo for fiscal year: {fy}");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@fy", fy);

                var result = await connection.QueryAsync<DealerstateModel>(
                    "sp_REPORT_StatewiseActivity",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var models = result.ToList();
                _logger.LogInformation($"NewDealerStatewiseReportRepo completed. Retrieved {models.Count} state records for fiscal year {fy}.");

                return models;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in NewDealerStatewiseReportRepo for fiscal year {fy}");
                _utility.ExcepLog(ex);
                throw;
            }
        }

        /// <summary>
        /// Retrieves a paginated list of demo requests based on filter criteria
        /// </summary>
        public async Task<(List<DemoListModel> DemoRequests, int TotalCount)> DemoTractorReportRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation($"Executing DemoTractorReportRepo with filter: FromDate={filter.From}, ToDate={filter.To}, RequestNo={filter.RequestNo}, PageIndex={pageIndex}, PageSize={pageSize}");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = CreateRequestParameters(filter, pageIndex, pageSize);

                using var multi = await connection.QueryMultipleAsync("sp_REPORT_DemoTractorLog", parameters, commandType: CommandType.StoredProcedure);

                var demoRequests = (await multi.ReadAsync<DemoListModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                _logger.LogInformation($"DemoTractorReportRepo completed. Retrieved {demoRequests.Count} demo requests out of {totalCount} total records.");

                return (demoRequests, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorReportRepo");
                throw new Exception("Error while retrieving demo request list.", ex);
            }
        }

        /// <summary>
        /// Retrieves a paginated list of new dealer activities based on filter criteria
        /// </summary>
        public async Task<(List<NewDealerActivity> NewDealerActivities, int TotalCount)> NewDealerActivityReportRepo(FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            _logger.LogInformation($"Executing NewDealerActivityReportRepo with filter: FromDate={filter.From}, ToDate={filter.To}, RequestNo={filter.RequestNo}, PendingByHO={pendingByHO}, Export={filter.Export}, PageIndex={pageIndex}, PageSize={pageSize}");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();

                // Add optional parameters only if they have values
                AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
                AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));
                AddParameterIfNotNull(parameters, "@RequestNo", filter.RequestNo);
                AddParameterIfNotNull(parameters, "@PendingByHO", pendingByHO);

                parameters.Add("@Export", filter.Export);
                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync("sp_REPORT_NewDealerActivityLog", parameters, commandType: CommandType.StoredProcedure);

                var newDealerActivities = (await multi.ReadAsync<NewDealerActivity>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                _logger.LogInformation($"NewDealerActivityReportRepo completed. Retrieved {newDealerActivities.Count} new dealer activities out of {totalCount} total records.");

                return (newDealerActivities, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NewDealerActivityReportRepo");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a paginated list of new dealer claim activities based on filter criteria
        /// </summary>
        public async Task<(List<NewDealerActivityClaim> NewDealerClaimActivities, int TotalCount)> NewDealerClaimReportRepo(FilterModel filter, int pageIndex, int pageSize)
        {
            _logger.LogInformation($"Executing NewDealerClaimReportRepo with filter: FromDate={filter.From}, ToDate={filter.To}, RequestNo={filter.RequestNo}, PageIndex={pageIndex}, PageSize={pageSize}");

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();

                // Fixed request type for dealer claims
                parameters.Add("@RequestTypeId", 2);

                // Add filter parameters
                AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
                AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));
                AddParameterIfNotNull(parameters, "@RequestNo", filter.RequestNo);

                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync("sp_REPORT_ClaimListNewDealer", parameters, commandType: CommandType.StoredProcedure);

                var newDealerClaimActivities = (await multi.ReadAsync<NewDealerActivityClaim>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                _logger.LogInformation($"NewDealerClaimReportRepo completed. Retrieved {newDealerClaimActivities.Count} new dealer claim activities out of {totalCount} total records.");

                return (newDealerClaimActivities, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NewDealerClaimReportRepo");
                throw;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Creates parameters for basic filter, pagination, and authentication
        /// </summary>
        private DynamicParameters CreateBasicParameters(FilterModel filter, int pageIndex, int pageSize)
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
        /// Creates parameters for date range filtering
        /// </summary>
        private DynamicParameters CreateDateParameters(FilterModel filter)
        {
            var parameters = new DynamicParameters();

            AddParameterIfNotNull(parameters, "@FromDate", filter.From?.ToString("yyyy-MM-dd"));
            AddParameterIfNotNull(parameters, "@ToDate", filter.To?.ToString("yyyy-MM-dd"));

            return parameters;
        }

        /// <summary>
        /// Creates parameters for request filtering and pagination
        /// </summary>
        private DynamicParameters CreateRequestParameters(FilterModel filter, int pageIndex, int pageSize)
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
        /// Adds a parameter to the DynamicParameters object only if the value is not null
        /// </summary>
        private void AddParameterIfNotNull(DynamicParameters parameters, string name, object value)
        {
            if (value != null)
            {
                parameters.Add(name, value);
            }
        }

        #endregion
    }
}