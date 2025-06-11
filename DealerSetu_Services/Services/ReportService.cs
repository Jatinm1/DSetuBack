using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Service responsible for generating and retrieving various types of reports in the DealerSetu system
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepository;

        // Constants for validation
        private const int MIN_PAGE_INDEX = 0;
        private const int MIN_PAGE_SIZE = 1;
        private const int MAX_PAGE_SIZE = 100000;
        private const int MIN_FISCAL_YEAR = 2000;
        private const int MAX_FISCAL_YEAR = 3000;

        /// <summary>
        /// Initializes a new instance of the ReportService
        /// </summary>
        /// <param name="reportRepository">Repository for report data operations</param>
        /// <exception cref="ArgumentNullException">Thrown when reportRepository is null</exception>
        public ReportService(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository ?? throw new ArgumentNullException(nameof(reportRepository));
        }

        /// <summary>
        /// Retrieves a paginated list of section reports based on the provided filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria including date range and other parameters</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>ServiceResponse containing paginated report data and total count</returns>
        public async Task<ServiceResponse> RequestSectionReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                // Validate input parameters
                var validationResponse = ValidatePaginationParameters(pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var filterValidationResponse = ValidateFilter(filter);
                if (filterValidationResponse != null)
                    return filterValidationResponse;

                var (reports, totalCount) = await _reportRepository.RequestSectionReportRepo(filter, pageIndex, pageSize);

                if (reports == null)
                {
                    return CreateErrorResponse("Repository returned null result", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = reports,
                    totalCount = totalCount,
                    Message = reports.Count > 0 ? "Reports retrieved successfully" : "No reports found for the specified criteria",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse($"Invalid argument: {ex.Message}", "400");
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResponse($"Operation failed: {ex.Message}", "500");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Unexpected error while retrieving reports: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves a list of rejected request reports based on the provided filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria including date range and other parameters</param>
        /// <returns>DemoTractor object containing rejected request data</returns>
        /// <exception cref="ArgumentNullException">Thrown when filter is null</exception>
        /// <exception cref="ArgumentException">Thrown when filter contains invalid data</exception>
        public async Task<DemoTractor> RejectedRequestReportService(FilterModel filter)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            try
            {
                ValidateFilterDates(filter);

                var result = await _reportRepository.RejectedRequestReportRepo(filter);

                if (result == null)
                    throw new InvalidOperationException("Repository returned null result for rejected requests");

                return result;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve rejected request reports: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves state-wise dealer information for a specified fiscal year
        /// </summary>
        /// <param name="fy">Fiscal year for which to retrieve dealer data</param>
        /// <returns>List of dealer state models containing state-wise dealer information</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when fiscal year is invalid</exception>
        /// <exception cref="InvalidOperationException">Thrown when operation fails</exception>
        public async Task<List<DealerstateModel>> NewDealerStatewiseReportService(int fy)
        {
            if (fy < MIN_FISCAL_YEAR || fy > MAX_FISCAL_YEAR)
                throw new ArgumentOutOfRangeException(nameof(fy), $"Fiscal year must be between {MIN_FISCAL_YEAR} and {MAX_FISCAL_YEAR}");

            try
            {
                var result = await _reportRepository.NewDealerStatewiseReportRepo(fy);

                if (result == null)
                    throw new InvalidOperationException("Repository returned null result for state-wise dealer data");

                return result;
            }
            catch (ArgumentOutOfRangeException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to retrieve state-wise dealer data for fiscal year {fy}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Retrieves a paginated list of demo tractor reports based on the provided filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria including date range, request number, and other parameters</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>ServiceResponse containing paginated demo request data and total count</returns>
        public async Task<ServiceResponse> DemoTractorReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                // Validate input parameters
                var validationResponse = ValidatePaginationParameters(pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var filterValidationResponse = ValidateFilter(filter);
                if (filterValidationResponse != null)
                    return filterValidationResponse;

                var (demoRequests, totalCount) = await _reportRepository.DemoTractorReportRepo(filter, pageIndex, pageSize);

                if (demoRequests == null)
                {
                    return CreateErrorResponse("Repository returned null result", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = demoRequests,
                    totalCount = totalCount,
                    Message = demoRequests.Count > 0 ? "Demo requests retrieved successfully" : "No demo requests found for the specified criteria",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse($"Invalid argument: {ex.Message}", "400");
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResponse($"Operation failed: {ex.Message}", "500");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Unexpected error while retrieving demo requests: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of new dealer activity reports based on the provided filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria including date range, request number, and export flag</param>
        /// <param name="pendingByHO">Optional flag to filter by Head Office pending status</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>ServiceResponse containing paginated new dealer activity data and total count</returns>
        public async Task<ServiceResponse> NewDealerActivityReportService(FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize)
        {
            try
            {
                // Validate input parameters
                var validationResponse = ValidatePaginationParameters(pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var filterValidationResponse = ValidateFilter(filter);
                if (filterValidationResponse != null)
                    return filterValidationResponse;

                var (newDealerActivities, totalCount) = await _reportRepository.NewDealerActivityReportRepo(filter, pendingByHO, pageIndex, pageSize);

                if (newDealerActivities == null)
                {
                    return CreateErrorResponse("Repository returned null result", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = newDealerActivities,
                    totalCount = totalCount,
                    Message = newDealerActivities.Count > 0 ? "New dealer activities retrieved successfully" : "No new dealer activities found for the specified criteria",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse($"Invalid argument: {ex.Message}", "400");
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResponse($"Operation failed: {ex.Message}", "500");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Unexpected error while retrieving new dealer activities: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of new dealer claim activity reports based on the provided filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria including date range and request number</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>ServiceResponse containing paginated new dealer claim activity data and total count</returns>
        public async Task<ServiceResponse> NewDealerClaimReportService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                // Validate input parameters
                var validationResponse = ValidatePaginationParameters(pageIndex, pageSize);
                if (validationResponse != null)
                    return validationResponse;

                var filterValidationResponse = ValidateFilter(filter);
                if (filterValidationResponse != null)
                    return filterValidationResponse;

                var (newDealerClaimActivities, totalCount) = await _reportRepository.NewDealerClaimReportRepo(filter, pageIndex, pageSize);

                if (newDealerClaimActivities == null)
                {
                    return CreateErrorResponse("Repository returned null result", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = newDealerClaimActivities,
                    totalCount = totalCount,
                    Message = newDealerClaimActivities.Count > 0 ? "New dealer claim activities retrieved successfully" : "No new dealer claim activities found for the specified criteria",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse($"Invalid argument: {ex.Message}", "400");
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResponse($"Operation failed: {ex.Message}", "500");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Unexpected error while retrieving new dealer claim activities: {ex.Message}", "500");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates pagination parameters
        /// </summary>
        /// <param name="pageIndex">Page index to validate</param>
        /// <param name="pageSize">Page size to validate</param>
        /// <returns>ServiceResponse with error details if validation fails, null if valid</returns>
        private static ServiceResponse ValidatePaginationParameters(int pageIndex, int pageSize)
        {
            if (pageIndex < MIN_PAGE_INDEX)
            {
                return CreateErrorResponse($"Page index must be greater than or equal to {MIN_PAGE_INDEX}", "400");
            }

            if (pageSize < MIN_PAGE_SIZE || pageSize > MAX_PAGE_SIZE)
            {
                return CreateErrorResponse($"Page size must be between {MIN_PAGE_SIZE} and {MAX_PAGE_SIZE}", "400");
            }

            return null;
        }

        /// <summary>
        /// Validates filter model
        /// </summary>
        /// <param name="filter">Filter to validate</param>
        /// <returns>ServiceResponse with error details if validation fails, null if valid</returns>
        private static ServiceResponse ValidateFilter(FilterModel filter)
        {
            if (filter == null)
            {
                return CreateErrorResponse("Filter cannot be null", "400");
            }

            try
            {
                ValidateFilterDates(filter);
                return null;
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
        }

        /// <summary>
        /// Validates filter date ranges
        /// </summary>
        /// <param name="filter">Filter containing dates to validate</param>
        /// <exception cref="ArgumentException">Thrown when date validation fails</exception>
        private static void ValidateFilterDates(FilterModel filter)
        {
            if (filter.From.HasValue && filter.To.HasValue)
            {
                if (filter.From.Value > filter.To.Value)
                {
                    throw new ArgumentException("From date cannot be greater than To date");
                }

                //var dateDifference = filter.To.Value - filter.From.Value;
                //if (dateDifference.TotalDays > 365)
                //{
                //    throw new ArgumentException("Date range cannot exceed 365 days");
                //}
            }

            //if (filter.From.HasValue && filter.From.Value > DateTime.Now)
            //{
            //    throw new ArgumentException("From date cannot be in the future");
            //}

            //if (filter.To.HasValue && filter.To.Value > DateTime.Now)
            //{
            //    throw new ArgumentException("To date cannot be in the future");
            //}
        }

        /// <summary>
        /// Creates a standardized error response
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code</param>
        /// <returns>ServiceResponse with error details</returns>
        private static ServiceResponse CreateErrorResponse(string message, string code)
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

        #endregion
    }
}