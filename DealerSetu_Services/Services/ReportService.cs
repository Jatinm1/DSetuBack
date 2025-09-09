using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        private const int MIN_PAGE_INDEX = 0;
        private const int MIN_PAGE_SIZE = 1;
        private const int MAX_PAGE_SIZE = 100000;
        private const int MIN_FISCAL_YEAR = 2000;
        private const int MAX_FISCAL_YEAR = 3000;

        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        }

        public async Task<ServiceResponse> RequestSectionReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var validationResponse = ValidateInputs(filter, pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var (reports, totalCount) = await _reportRepository.RequestSectionReportRepo(filter, pageIndex, pageSize);

                if (reports == null)
                    return CreateErrorResponse("Unable to retrieve reports", "500");

                return CreateSuccessResponse(reports, totalCount, "Reports retrieved successfully", "No reports found for the specified criteria");
            }
            catch
            {
                return CreateErrorResponse("An error occurred while retrieving reports", "500");
            }
        }

        public async Task<DemoTractor> RejectedRequestReportService(FilterModel filter)
        {
            try
            {
                if (filter == null)
                    throw new ArgumentNullException(nameof(filter));

                ValidateFilterDates(filter);
                var result = await _reportRepository.RejectedRequestReportRepo(filter);

                if (result == null)
                    throw new InvalidOperationException("Repository returned null result for rejected requests");

                return result;
            }
            catch
            {
                throw new InvalidOperationException("Failed to retrieve rejected request reports");
            }
        }

        public async Task<List<DealerstateModel>> NewDealerStatewiseReportService(int fy)
        {
            try
            {
                if (fy < MIN_FISCAL_YEAR || fy > MAX_FISCAL_YEAR)
                    throw new ArgumentOutOfRangeException(nameof(fy), "Invalid fiscal year");

                var result = await _reportRepository.NewDealerStatewiseReportRepo(fy);

                if (result == null)
                    throw new InvalidOperationException("Repository returned null result for state-wise dealer data");

                return result;
            }
            catch
            {
                throw new InvalidOperationException("Failed to retrieve state-wise dealer data");
            }
        }

        public async Task<ServiceResponse> DemoTractorReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var validationResponse = ValidateInputs(filter, pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var (demoRequests, totalCount) = await _reportRepository.DemoTractorReportRepo(filter, pageIndex, pageSize);

                if (demoRequests == null)
                    return CreateErrorResponse("Unable to retrieve demo requests", "500");

                return CreateSuccessResponse(demoRequests, totalCount, "Demo requests retrieved successfully", "No demo requests found for the specified criteria");
            }
            catch
            {
                return CreateErrorResponse("An error occurred while retrieving demo requests", "500");
            }
        }

        public async Task<ServiceResponse> NewDealerActivityReportService(FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            try
            {
                var validationResponse = ValidateInputs(filter, pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var (newDealerActivities, totalCount) = await _reportRepository.NewDealerActivityReportRepo(filter, pendingByHO, pageIndex, pageSize);

                if (newDealerActivities == null)
                    return CreateErrorResponse("Unable to retrieve new dealer activities", "500");

                return CreateSuccessResponse(newDealerActivities, totalCount, "New dealer activities retrieved successfully", "No new dealer activities found for the specified criteria");
            }
            catch
            {
                return CreateErrorResponse("An error occurred while retrieving new dealer activities", "500");
            }
        }

        public async Task<ServiceResponse> NewDealerClaimReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var validationResponse = ValidateInputs(filter, pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var (newDealerClaimActivities, totalCount) = await _reportRepository.NewDealerClaimReportRepo(filter, pageIndex, pageSize);

                if (newDealerClaimActivities == null)
                    return CreateErrorResponse("Unable to retrieve new dealer claim activities", "500");

                return CreateSuccessResponse(newDealerClaimActivities, totalCount, "New dealer claim activities retrieved successfully", "No new dealer claim activities found for the specified criteria");
            }
            catch
            {
                return CreateErrorResponse("An error occurred while retrieving new dealer claim activities", "500");
            }
        }

        #region Private Helper Methods

        private ServiceResponse ValidateInputs(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                if (pageIndex < MIN_PAGE_INDEX)
                    return CreateErrorResponse("Invalid page index", "400");

                if (pageSize < MIN_PAGE_SIZE || pageSize > MAX_PAGE_SIZE)
                    return CreateErrorResponse("Invalid page size", "400");

                if (filter == null)
                    return CreateErrorResponse("Filter cannot be null", "400");

                ValidateFilterDates(filter);
                return null;
            }
            catch
            {
                return CreateErrorResponse("Invalid input parameters", "400");
            }
        }

        private static void ValidateFilterDates(FilterModel filter)
        {
            try
            {
                if (filter.From.HasValue && filter.To.HasValue && filter.From.Value > filter.To.Value)
                    throw new ArgumentException("From date cannot be greater than To date");
            }
            catch
            {
                throw new ArgumentException("Invalid date range");
            }
        }

        private static ServiceResponse CreateErrorResponse(string message, string code)
        {
            try
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = message,
                    Message = "Operation failed",
                    Code = code,
                    Status = "Error",
                    result = null,
                    totalCount = 0
                };
            }
            catch
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = "An error occurred",
                    Message = "Operation failed",
                    Code = "500",
                    Status = "Error",
                    result = null,
                    totalCount = 0
                };
            }
        }

        private static ServiceResponse CreateSuccessResponse(object data, int totalCount, string successMessage, string noDataMessage)
        {
            try
            {
                var count = data switch
                {
                    System.Collections.ICollection collection => collection.Count,
                    _ => totalCount
                };

                return new ServiceResponse
                {
                    isError = false,
                    result = data,
                    totalCount = totalCount,
                    Message = count > 0 ? successMessage : noDataMessage,
                    Code = "200",
                    Status = "Success"
                };
            }
            catch
            {
                return new ServiceResponse
                {
                    isError = false,
                    result = data,
                    totalCount = totalCount,
                    Message = "Data retrieved",
                    Code = "200",
                    Status = "Success"
                };
            }
        }

        #endregion
    }
}