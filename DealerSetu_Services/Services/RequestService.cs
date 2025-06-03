using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using DealerSetu_Data.Models.HelperModels;
using System;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Service responsible for handling request-related operations including submission, retrieval, and filtering
    /// </summary>
    public class RequestService : IRequestService
    {
        private readonly IRequestRepository _requestRepository;

        // Constants for validation
        private const int MIN_PAGE_INDEX = 0;
        private const int MIN_PAGE_SIZE = 1;
        private const int MAX_PAGE_SIZE = 1000;
        private const int MAX_MESSAGE_LENGTH = 2000;
        private const int MAX_EMPLOYEE_NUMBER_LENGTH = 20;

        /// <summary>
        /// Initializes a new instance of the RequestService
        /// </summary>
        /// <param name="requestRepository">Repository for request data operations</param>
        /// <exception cref="ArgumentNullException">Thrown when requestRepository is null</exception>
        public RequestService(IRequestRepository requestRepository)
        {
            _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        }

        /// <summary>
        /// Retrieves all available request types for filtering purposes
        /// </summary>
        /// <returns>ServiceResponse containing list of request types or error information</returns>
        public async Task<ServiceResponse> RequestTypeFilterService()
        {
            try
            {
                var requestTypes = await _requestRepository.RequestTypeFilterRepo();

                if (requestTypes == null)
                {
                    return CreateErrorResponse("Repository returned null result for request types", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = requestTypes,
                    Message = "Request types retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResponse($"Operation failed: {ex.Message}", "500");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Unexpected error while retrieving request types: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves all available HP (Horsepower) categories for filtering and selection
        /// </summary>
        /// <returns>ServiceResponse containing list of HP categories or error information</returns>
        public async Task<ServiceResponse> HPCategoryService()
        {
            try
            {
                var hpCategories = await _requestRepository.HPCategoryFilterRepo();

                if (hpCategories == null)
                {
                    return CreateErrorResponse("Repository returned null result for HP categories", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = hpCategories,
                    Message = "HP categories retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (InvalidOperationException ex)
            {
                return CreateErrorResponse($"Operation failed: {ex.Message}", "500");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Unexpected error while retrieving HP categories: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Submits a new request with the specified parameters
        /// </summary>
        /// <param name="requestTypeId">Unique identifier for the request type</param>
        /// <param name="message">Request message or description</param>
        /// <param name="hpCategory">HP category for the request</param>
        /// <param name="empNo">Employee number of the requester</param>
        /// <returns>ServiceResponse containing the generated request ID or error information</returns>
        public async Task<ServiceResponse> SubmitRequestService(string requestTypeId, string message, string hpCategory, string empNo)
        {
            try
            {
                // Validate input parameters
                var validationResponse = ValidateSubmissionParameters(requestTypeId, message, hpCategory, empNo);
                if (validationResponse != null)
                    return validationResponse;

                // Submit the request and get result and notification data
                var submissionResult = await _requestRepository.SubmitRequestAsync(requestTypeId, message, hpCategory, empNo);

                if (submissionResult == null)
                {
                    return CreateErrorResponse("Repository returned null result for request submission", "500");
                }

                if (submissionResult.RequestId <= 0)
                {
                    return CreateErrorResponse("Invalid request ID generated during submission", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = $"Your RequestId is : {submissionResult.RequestId}",
                    Message = "Request submitted successfully",
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
                return CreateErrorResponse($"Unexpected error while submitting request: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves a paginated list of requests based on the provided filter criteria
        /// </summary>
        /// <param name="filter">Filter criteria for request retrieval</param>
        /// <param name="pageIndex">Zero-based page index for pagination</param>
        /// <param name="pageSize">Number of records per page</param>
        /// <returns>ServiceResponse containing paginated request data and total count</returns>
        public async Task<ServiceResponse> RequestListService(FilterModel filter, int pageIndex, int pageSize)
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

                var (requests, totalCount) = await _requestRepository.RequestListRepo(filter, pageIndex, pageSize);

                if (requests == null)
                {
                    return CreateErrorResponse("Repository returned null result for request list", "500");
                }

                return new ServiceResponse
                {
                    isError = false,
                    result = requests,
                    totalCount = totalCount,
                    Message = requests.Count > 0 ? "Requests retrieved successfully" : "No requests found for the specified criteria",
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
                return CreateErrorResponse($"Unexpected error while retrieving requests: {ex.Message}", "500");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates parameters for request submission
        /// </summary>
        /// <param name="requestTypeId">Request type ID to validate</param>
        /// <param name="message">Message to validate</param>
        /// <param name="hpCategory">HP category to validate</param>
        /// <param name="empNo">Employee number to validate</param>
        /// <returns>ServiceResponse with error details if validation fails, null if valid</returns>
        private static ServiceResponse ValidateSubmissionParameters(string requestTypeId, string message, string hpCategory, string empNo)
        {
            if (string.IsNullOrWhiteSpace(requestTypeId))
            {
                return CreateErrorResponse("Request type ID is required", "400");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return CreateErrorResponse("Message is required", "400");
            }

            if (message.Length > MAX_MESSAGE_LENGTH)
            {
                return CreateErrorResponse($"Message cannot exceed {MAX_MESSAGE_LENGTH} characters", "400");
            }

            //if (string.IsNullOrWhiteSpace(hpCategory))
            //{
            //    return CreateErrorResponse("HP category is required", "400");
            //}

            if (string.IsNullOrWhiteSpace(empNo))
            {
                return CreateErrorResponse("Employee number is required", "400");
            }

            if (empNo.Length > MAX_EMPLOYEE_NUMBER_LENGTH)
            {
                return CreateErrorResponse($"Employee number cannot exceed {MAX_EMPLOYEE_NUMBER_LENGTH} characters", "400");
            }

            // Basic sanitization checks
            if (ContainsInvalidCharacters(requestTypeId) || ContainsInvalidCharacters(hpCategory) || ContainsInvalidCharacters(empNo))
            {
                return CreateErrorResponse("Invalid characters detected in input parameters", "400");
            }

            return null;
        }

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

                var dateDifference = filter.To.Value - filter.From.Value;
                if (dateDifference.TotalDays > 365)
                {
                    throw new ArgumentException("Date range cannot exceed 365 days");
                }
            }

            if (filter.From.HasValue && filter.From.Value > DateTime.Now)
            {
                throw new ArgumentException("From date cannot be in the future");
            }

            if (filter.To.HasValue && filter.To.Value > DateTime.Now)
            {
                throw new ArgumentException("To date cannot be in the future");
            }
        }

        /// <summary>
        /// Checks for potentially dangerous characters in input strings
        /// </summary>
        /// <param name="input">String to validate</param>
        /// <returns>True if invalid characters are found, false otherwise</returns>
        private static bool ContainsInvalidCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            // Check for common injection patterns and dangerous characters
            var dangerousPatterns = new[] { "<script", "javascript:", "onload=", "onerror=", "'", "\"", ";", "--", "/*", "*/" };

            return dangerousPatterns.Any(pattern => input.ToLowerInvariant().Contains(pattern.ToLowerInvariant()));
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




        //private async Task SendEmailNotificationsAsync(RequestSubmissionResult result)
        //{
        //    // Send email to HO users
        //    if (result.HOEmails?.Count > 0)
        //    {
        //        string subject = $"SETU PORTAL: {result.RequestTypeName}";
        //        string emailMessage = $@"New request (type of request: <strong>{result.RequestTypeName}</strong>) generated by dealer: {result.DealerName} ({result.EmpNo}).
        //                                <br />Location: {result.DealerLocation} State: {result.DealerState}
        //                                <br /> Kindly respond.";

        //        await _emailService.SendEmailAsync(
        //            subject,
        //            result.HOEmails,
        //            result.CCEmails ?? new List<EmailModel>(),
        //            emailMessage,
        //            result.RequestNo,
        //            DateTime.Now.ToString("dd MMM yyyy hh:mm tt")
        //        );
        //    }

        //    // Send confirmation email to dealer
        //    if (result.DealerEmail != null)
        //    {
        //        string subjectDealer = "Your Request has been created";
        //        string emailMessageDealer = $@"New request (type of request: <strong>{result.RequestTypeName}</strong>) has been created by you: {result.DealerName} ({result.EmpNo}).
        //                                    <br />Location: {result.DealerLocation} State: {result.DealerState}
        //                                    <br /> It is now pending on next stage.";

        //        await _emailService.SendEmailAsync(
        //            subjectDealer,
        //            new List<EmailModel> { result.DealerEmail },
        //            new List<EmailModel>(),
        //            emailMessageDealer,
        //            result.RequestNo,
        //            DateTime.Now.ToString("dd MMM yyyy hh:mm tt")
        //        );
        //    }
        //}

