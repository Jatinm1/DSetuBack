using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;
        private readonly ILogger<ReportService> _logger;

        public ReportService(IReportRepository reportRepository, ILogger<ReportService> logger)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Retrieves a paginated list of reports based on the provided filter
        /// </summary>
        public async Task<ServiceResponse> RequestSectionReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Retrieving reports with filter: FromDate={filter.From}, ToDate={filter.To}, PageIndex={pageIndex}, PageSize={pageSize}");

                var (reports, totalCount) = await _reportRepository.RequestSectionReportRepo(filter, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = reports,
                    totalCount = totalCount,
                    Message = reports.Count > 0 ? "Reports retrieved successfully" : "No reports found",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RequestSectionReportService");
                return CreateErrorResponse(ex, "Error while retrieving reports");
            }
        }

        /// <summary>
        /// Retrieves a list of rejected requests
        /// </summary>
        public async Task<DemoTractor> RejectedRequestReportService(FilterModel filter)
        {
            try
            {
                _logger.LogInformation($"Retrieving rejected requests with filter: FromDate={filter.From}, ToDate={filter.To}");
                return await _reportRepository.RejectedRequestReportRepo(filter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in RejectedRequestReportService");
                throw;
            }
        }

        /// <summary>
        /// Retrieves state-wise dealer information for a given fiscal year
        /// </summary>
        public async Task<List<DealerstateModel>> NewDealerStatewiseReportService(int fy)
        {
            try
            {
                _logger.LogInformation($"Retrieving state-wise dealer data for fiscal year: {fy}");
                return await _reportRepository.NewDealerStatewiseReportRepo(fy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in NewDealerStatewiseReportService for fiscal year {fy}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a paginated list of demo requests based on the provided filter
        /// </summary>
        public async Task<ServiceResponse> DemoTractorReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Retrieving demo requests with filter: FromDate={filter.From}, ToDate={filter.To}, RequestNo={filter.RequestNo}, PageIndex={pageIndex}, PageSize={pageSize}");

                var (demoRequests, totalCount) = await _reportRepository.DemoTractorReportRepo(filter, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = demoRequests,
                    totalCount = totalCount,
                    Message = demoRequests.Count > 0 ? "Demo requests retrieved successfully" : "No demo requests found",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DemoTractorReportService");
                return CreateErrorResponse(ex, "Error while retrieving demo requests");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of new dealer activities based on the provided filter
        /// </summary>
        public async Task<ServiceResponse> NewDealerActivityReportService(FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Retrieving new dealer activities with filter: FromDate={filter.From}, ToDate={filter.To}, RequestNo={filter.RequestNo}, PendingByHO={pendingByHO}, Export={filter.Export}, PageIndex={pageIndex}, PageSize={pageSize}");

                var (newDealerActivities, totalCount) = await _reportRepository.NewDealerActivityReportRepo(filter, pendingByHO, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = newDealerActivities,
                    totalCount = totalCount,
                    Message = newDealerActivities.Count > 0 ? "New dealer activities retrieved successfully" : "No new dealer activities found",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NewDealerActivityReportService");
                return CreateErrorResponse(ex, "Error while retrieving new dealer activities");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of new dealer claim activities based on the provided filter
        /// </summary>
        public async Task<ServiceResponse> NewDealerClaimReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                _logger.LogInformation($"Retrieving new dealer claim activities with filter: FromDate={filter.From}, ToDate={filter.To}, RequestNo={filter.RequestNo}, PageIndex={pageIndex}, PageSize={pageSize}");

                var (newDealerClaimActivities, totalCount) = await _reportRepository.NewDealerClaimReportRepo(filter, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = newDealerClaimActivities,
                    totalCount = totalCount,
                    Message = newDealerClaimActivities.Count > 0 ? "New dealer claim activities retrieved successfully" : "No new dealer claim activities found",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NewDealerClaimReportService");
                return CreateErrorResponse(ex, "Error while retrieving new dealer claim activities");
            }
        }

        #region Helper Methods

        private ServiceResponse CreateErrorResponse(Exception ex, string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = ex.Message,
                Message = message,
                Code = "500",
                Status = "Error"
            };
        }

        #endregion
    }
}