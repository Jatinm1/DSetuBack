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

        public ReportRepository(IConfiguration configuration, Utility utility)
        {
            _connectionString = configuration?.GetConnectionString("dbDealerSetuEntities")
                ?? throw new InvalidOperationException("Connection string 'dbDealerSetuEntities' not found.");
            _utility = utility ?? throw new ArgumentNullException(nameof(utility));
        }

        public async Task<(List<ReportModel> Reports, int TotalCount)> RequestSectionReportRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (pageIndex < 0)
                throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be positive", nameof(pageSize));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                if (filter.From.HasValue)
                    parameters.Add("@FromDate", filter.From.Value.ToString("yyyy-MM-dd"));
                if (filter.To.HasValue)
                    parameters.Add("@ToDate", filter.To.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(filter.EmpNo))
                    parameters.Add("@EmpNo", filter.EmpNo);
                if (filter.RoleId!=null && !string.IsNullOrEmpty(filter.RoleId))
                    parameters.Add("@RoleId", filter.RoleId);

                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_LogForRequests",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var reports = (await multi.ReadAsync<ReportModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (reports, totalCount);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving report data", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing report request", ex);
            }
        }

        public async Task<DemoTractor> RejectedRequestReportRepo(FilterModel filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                if (filter.From.HasValue)
                    parameters.Add("@FromDate", filter.From.Value.ToString("yyyy-MM-dd"));
                if (filter.To.HasValue)
                    parameters.Add("@ToDate", filter.To.Value.ToString("yyyy-MM-dd"));

                // Fetch rejected claim plans
                var claimPlans = (await connection.QueryAsync<Rejectrequest>(
                    "sp_REPORT_RejectedClaimPlans",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                // Fetch rejected demo tractors using the same parameters
                var demoTractors = (await connection.QueryAsync<DemoTractorReject>(
                    "sp_REPORT_RejectedDemoTractors",
                    parameters,
                    commandType: CommandType.StoredProcedure
                )).ToList();

                return new DemoTractor
                {
                    newdealercount = claimPlans.Count,
                    democount = demoTractors.Count
                };
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving rejected request data", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing rejected request report", ex);
            }
        }

        public async Task<List<DealerstateModel>> NewDealerStatewiseReportRepo(int fy)
        {
            if (fy <= 0)
                throw new ArgumentOutOfRangeException(nameof(fy), "Fiscal year must be a positive integer");

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
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException($"Database error occurred while retrieving state-wise data for fiscal year {fy}", ex);
            }
            catch (Exception ex)
            {
                _utility.ExcepLog(ex);
                throw new InvalidOperationException($"Unexpected error occurred while processing state-wise report for fiscal year {fy}", ex);
            }
        }

        public async Task<(List<DemoListModel> DemoRequests, int TotalCount)> DemoTractorReportRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (pageIndex < 0)
                throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be positive", nameof(pageSize));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                if (filter.From.HasValue)
                    parameters.Add("@FromDate", filter.From.Value.ToString("yyyy-MM-dd"));
                if (filter.To.HasValue)
                    parameters.Add("@ToDate", filter.To.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(filter.RequestNo))
                    parameters.Add("@RequestNo", filter.RequestNo);

                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_DemoTractorLog",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var demoRequests = (await multi.ReadAsync<DemoListModel>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (demoRequests, totalCount);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving demo tractor data", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing demo tractor report", ex);
            }
        }

        public async Task<(List<NewDealerActivity> NewDealerActivities, int TotalCount)> NewDealerActivityReportRepo(
            FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (pageIndex < 0)
                throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be positive", nameof(pageSize));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                if (filter.From.HasValue)
                    parameters.Add("@FromDate", filter.From.Value.ToString("yyyy-MM-dd"));
                if (filter.To.HasValue)
                    parameters.Add("@ToDate", filter.To.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(filter.RequestNo))
                    parameters.Add("@RequestNo", filter.RequestNo);
                if (pendingByHO.HasValue)
                    parameters.Add("@PendingByHO", pendingByHO.Value);

                parameters.Add("@Export", filter.Export);
                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_NewDealerActivityLog",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var activities = (await multi.ReadAsync<NewDealerActivity>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (activities, totalCount);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving new dealer activity data", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing new dealer activity report", ex);
            }
        }

        public async Task<(List<NewDealerActivityClaim> NewDealerClaimActivities, int TotalCount)> NewDealerClaimReportRepo(
            FilterModel filter, int pageIndex, int pageSize)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (pageIndex < 0)
                throw new ArgumentException("Page index must be non-negative", nameof(pageIndex));

            if (pageSize <= 0)
                throw new ArgumentException("Page size must be positive", nameof(pageSize));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();

                parameters.Add("@RequestTypeId", 2); // Fixed request type for dealer claims

                if (filter.From.HasValue)
                    parameters.Add("@FromDate", filter.From.Value.ToString("yyyy-MM-dd"));
                if (filter.To.HasValue)
                    parameters.Add("@ToDate", filter.To.Value.ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(filter.RequestNo))
                    parameters.Add("@RequestNo", filter.RequestNo);

                parameters.Add("@PageIndex", pageIndex);
                parameters.Add("@PageSize", pageSize);

                using var multi = await connection.QueryMultipleAsync(
                    "sp_REPORT_ClaimListNewDealer",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var claimActivities = (await multi.ReadAsync<NewDealerActivityClaim>()).ToList();
                var totalCount = (await multi.ReadAsync<int>()).FirstOrDefault();

                return (claimActivities, totalCount);
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (SqlException ex)
            {
                throw new InvalidOperationException("Database error occurred while retrieving new dealer claim data", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unexpected error occurred while processing new dealer claim report", ex);
            }
        }
    }
}