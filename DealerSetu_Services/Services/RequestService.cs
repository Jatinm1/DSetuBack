using DealerSetu_Data.Common;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Service for handling request operations including submission, retrieval, and filtering
    /// </summary>
    public class RequestService : IRequestService
    {
        private readonly IRequestRepository _requestRepository;
        private readonly ValidationHelper _validationHelper;

        private const int MAX_MESSAGE_LENGTH = 2000;
        private const int MAX_EMPLOYEE_NUMBER_LENGTH = 20;

        public RequestService(IRequestRepository requestRepository, ValidationHelper validationHelper)
        {
            _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
            _validationHelper = validationHelper;
        }

        /// <summary>
        /// Retrieves all available request types for filtering
        /// </summary>
        public async Task<ServiceResponse> RequestTypeFilterService()
        {
            try
            {
                var requestTypes = await _requestRepository.RequestTypeFilterRepo();
                return requestTypes == null
                    ? CreateErrorResponse("Repository returned null result for request types", "500")
                    : CreateSuccessResponse(requestTypes, "Request types retrieved successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error retrieving request types: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves all available HP categories for filtering
        /// </summary>
        public async Task<ServiceResponse> HPCategoryService()
        {
            try
            {
                var hpCategories = await _requestRepository.HPCategoryFilterRepo();
                return hpCategories == null
                    ? CreateErrorResponse("Repository returned null result for HP categories", "500")
                    : CreateSuccessResponse(hpCategories, "HP categories retrieved successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error retrieving HP categories: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Submits a new request with validation
        /// </summary>
        public async Task<ServiceResponse> SubmitRequestService(RequestSubmissionModel request, string empNo, string roleId)
        {
            try
            {
                var validationResponse = ValidateSubmissionParameters(request, empNo, roleId);
                if (validationResponse != null)
                    return validationResponse;

                var submissionResult = await _requestRepository.SubmitRequestAsync(request, empNo, roleId);

                if (submissionResult == null || submissionResult.RequestId <= 0)
                    return CreateErrorResponse("Invalid request ID generated during submission", "500");

                return CreateSuccessResponse($"Your RequestId is : {submissionResult.RequestId}", "Request submitted successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse($"Invalid argument: {ex.Message}", "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error submitting request: {ex.Message}", "500");
            }
        }

        /// <summary>
        /// Retrieves paginated list of requests with validation
        /// </summary>
        public async Task<ServiceResponse> RequestListService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var validationResponse = _validationHelper.ValidatePagination(pageIndex, pageSize) ??
                                       _validationHelper.ValidateDateRange(filter.From, filter.To) ??
                                       _validationHelper.ValidateRequestNo(filter.RequestNo);

                if (validationResponse != null)
                    return validationResponse;

                var (requests, totalCount) = await _requestRepository.RequestListRepo(filter, pageIndex, pageSize);

                if (requests == null)
                    return CreateErrorResponse("Repository returned null result for request list", "500");

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
            catch (Exception ex)
            {
                return CreateErrorResponse($"Error retrieving requests: {ex.Message}", "500");
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Validates request submission parameters
        /// </summary>
        private static ServiceResponse ValidateSubmissionParameters(RequestSubmissionModel request, string empNo, string roleId)
        {
            if (string.IsNullOrWhiteSpace(request.RequestTypeId))
                return CreateErrorResponse("Request type ID is required", "400");

            if (string.IsNullOrWhiteSpace(request.Message))
                return CreateErrorResponse("Message is required", "400");

            if (request.Message.Length > MAX_MESSAGE_LENGTH)
                return CreateErrorResponse($"Message cannot exceed {MAX_MESSAGE_LENGTH} characters", "400");

            if (string.IsNullOrWhiteSpace(empNo))
                return CreateErrorResponse("Employee number is required", "400");

            if (empNo.Length > MAX_EMPLOYEE_NUMBER_LENGTH)
                return CreateErrorResponse($"Employee number cannot exceed {MAX_EMPLOYEE_NUMBER_LENGTH} characters", "400");

            if (ContainsInvalidCharacters(request.RequestTypeId) ||
                ContainsInvalidCharacters(request.HpCategory) ||
                ContainsInvalidCharacters(empNo))
                return CreateErrorResponse("Invalid characters detected in input parameters", "400");

            return null;
        }

        /// <summary>
        /// Checks for potentially dangerous characters in input strings
        /// </summary>
        private static bool ContainsInvalidCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            var dangerousPatterns = new[] { "<script", "javascript:", "onload=", "onerror=", "'", "\"", ";", "--", "/*", "*/" };
            return dangerousPatterns.Any(pattern => input.ToLowerInvariant().Contains(pattern.ToLowerInvariant()));
        }

        /// <summary>
        /// Creates standardized success response
        /// </summary>
        private static ServiceResponse CreateSuccessResponse(object result, string message)
        {
            return new ServiceResponse
            {
                isError = false,
                result = result,
                Message = message,
                Status = "Success",
                Code = "200"
            };
        }

        /// <summary>
        /// Creates standardized error response
        /// </summary>
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


///// <summary>
///// Validates pagination parameters
///// </summary>
///// <param name="pageIndex">Page index to validate</param>
///// <param name="pageSize">Page size to validate</param>
///// <returns>ServiceResponse with error details if validation fails, null if valid</returns>
//private static ServiceResponse ValidatePaginationParameters(int pageIndex, int pageSize)
//{
//    if (pageIndex < MIN_PAGE_INDEX)
//    {
//        return CreateErrorResponse($"Page index must be greater than or equal to {MIN_PAGE_INDEX}", "400");
//    }

//    if (pageSize < MIN_PAGE_SIZE || pageSize > MAX_PAGE_SIZE)
//    {
//        return CreateErrorResponse($"Page size must be between {MIN_PAGE_SIZE} and {MAX_PAGE_SIZE}", "400");
//    }

//    return null;
//}

///// <summary>
///// Validates filter model
///// </summary>
///// <param name="filter">Filter to validate</param>
///// <returns>ServiceResponse with error details if validation fails, null if valid</returns>
//private static ServiceResponse ValidateFilter(FilterModel filter)
//{
//    if (filter == null)
//    {
//        return CreateErrorResponse("Filter cannot be null", "400");
//    }

//    try
//    {
//        ValidateFilterDates(filter);
//        return null;
//    }
//    catch (ArgumentException ex)
//    {
//        return CreateErrorResponse(ex.Message, "400");
//    }
//}

///// <summary>
///// Validates filter date ranges
///// </summary>
///// <param name="filter">Filter containing dates to validate</param>
///// <exception cref="ArgumentException">Thrown when date validation fails</exception>
//private static void ValidateFilterDates(FilterModel filter)
//{
//    if (filter.From.HasValue && filter.To.HasValue)
//    {
//        if (filter.From.Value > filter.To.Value)
//        {
//            throw new ArgumentException("From date cannot be greater than To date");
//        }

//        var dateDifference = filter.To.Value - filter.From.Value;
//        if (dateDifference.TotalDays > 365)
//        {
//            throw new ArgumentException("Date range cannot exceed 365 days");
//        }
//    }

//    if (filter.From.HasValue && filter.From.Value > DateTime.Now)
//    {
//        throw new ArgumentException("From date cannot be in the future");
//    }

//    if (filter.To.HasValue && filter.To.Value > DateTime.Now)
//    {
//        throw new ArgumentException("To date cannot be in the future");
//    }
//}