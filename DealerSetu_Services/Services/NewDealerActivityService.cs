using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using DealerSetu_Repositories.Repositories;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Services.Services
{
    /// <summary>
    /// Service for managing new dealer activities including claim submission, approval, and actual claim processing.
    /// Provides comprehensive functionality for dealer activity lifecycle management.
    /// </summary>
    public class NewDealerActivityService : INewDealerActivityService
    {
        #region Private Fields

        private readonly INewDealerActivityRepository _newDealerRepo;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IFileValidationService _fileValidationService;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the NewDealerActivityService.
        /// </summary>
        /// <param name="newDealerRepo">Repository for new dealer activities</param>
        /// <param name="blobStorageService">Service for blob storage operations</param>
        /// <param name="fileValidationService">Service for file validation</param>
        /// <exception cref="ArgumentNullException">Thrown when any required dependency is null</exception>
        public NewDealerActivityService(
            INewDealerActivityRepository newDealerRepo,
            IBlobStorageService blobStorageService,
            IFileValidationService fileValidationService)
        {
            _newDealerRepo = newDealerRepo ?? throw new ArgumentNullException(nameof(newDealerRepo));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
            _fileValidationService = fileValidationService ?? throw new ArgumentNullException(nameof(fileValidationService));
        }

        #endregion

        #region New Dealer Claim Services

        /// <summary>
        /// Retrieves a paginated list of new dealer activities based on the provided filter criteria.
        /// </summary>
        /// <param name="filter">Filter criteria for activities</param>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Service response containing the list of activities and total count</returns>
        public async Task<ServiceResponse> NewDealerActivityListService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                ValidatePaginationParameters(pageIndex, pageSize);

                var (newActivities, totalCount) = await _newDealerRepo.NewDealerActivityRepo(filter, pageIndex, pageSize);

                return CreateSuccessResponse(newActivities, totalCount, "New Activities retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error while retrieving New Activities", "500", ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a paginated list of pending new dealer activities.
        /// </summary>
        /// <param name="filter">Filter criteria for pending activities</param>
        /// <param name="pageIndex">Zero-based page index</param>
        /// <param name="pageSize">Number of items per page</param>
        /// <returns>Service response containing the list of pending activities and total count</returns>
        public async Task<ServiceResponse> NewDealerPendingListService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                ValidatePaginationParameters(pageIndex, pageSize);

                var (newPendingActivities, totalCount) = await _newDealerRepo.NewDealerPendingListRepo(filter, pageIndex, pageSize);

                return CreateSuccessResponse(newPendingActivities, totalCount, "New Pending Activities retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error while retrieving New Pending Activities", "500", ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the list of available dealer states.
        /// </summary>
        /// <returns>Service response containing the list of states</returns>
        public async Task<ServiceResponse> DealerStatesService()
        {
            try
            {
                var states = await _newDealerRepo.DealerStatesRepo();
                return CreateSuccessResponse(states, "States retrieved successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error retrieving states", "500", ex.Message);
            }
        }

        /// <summary>
        /// Retrieves dealer data for a specific request number.
        /// </summary>
        /// <param name="requestNo">The request number to retrieve data for</param>
        /// <returns>Service response containing dealer data</returns>
        /// <exception cref="ArgumentNullException">Thrown when requestNo is null or empty</exception>
        public async Task<ServiceResponse> DealerDataService(string requestNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(requestNo))
                    throw new ArgumentNullException(nameof(requestNo), "Request number cannot be null or empty");

                var dealerData = await _newDealerRepo.DealerDataRepo(requestNo);
                return CreateSuccessResponse(dealerData, "Dealer Data retrieved successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error retrieving Dealer Data", "500", ex.Message);
            }
        }

        /// <summary>
        /// Submits a new claim with activity data after validation.
        /// </summary>
        /// <param name="requestNo">The request number</param>
        /// <param name="dealerNo">The dealer number</param>
        /// <param name="activityData">List of activities to submit</param>
        /// <param name="empNo">Employee number</param>
        /// <returns>Service response containing the claim ID</returns>
        public async Task<ServiceResponse> SubmitClaimService(string requestNo, string dealerNo, List<ActivityModel> activityData, string empNo)
        {
            try
            {
                // Validate input parameters
                var validationResult = ValidateClaimSubmissionParameters(requestNo, dealerNo, activityData, empNo);
                if (!validationResult.IsValid)
                {
                    return CreateErrorResponse(validationResult.ErrorMessage, "400");
                }

                // Validate activity data for malicious content
                var maliciousValidationResult = ValidateActivityDataForMaliciousContent(activityData);
                if (!maliciousValidationResult.IsValid)
                {
                    return CreateErrorResponse(maliciousValidationResult.ErrorMessage, "400");
                }

                var claimId = await _newDealerRepo.SubmitClaimRepo(requestNo, dealerNo, activityData, empNo);
                return CreateSuccessResponse(claimId, "Claim Submitted successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error submitting Claim", "500", ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing claim with new activity data.
        /// </summary>
        /// <param name="claimId">The claim ID to update</param>
        /// <param name="activityData">Updated activity data</param>
        /// <param name="empNo">Employee number</param>
        /// <returns>Service response containing the updated claim ID</returns>
        public async Task<ServiceResponse> UpdateClaimService(int claimId, List<ActivityModel> activityData, string empNo)
        {
            try
            {
                if (claimId <= 0)
                    throw new ArgumentException("Claim ID must be greater than zero", nameof(claimId));

                if (string.IsNullOrWhiteSpace(empNo))
                    throw new ArgumentNullException(nameof(empNo), "Employee number cannot be null or empty");


                // Validate activity data for malicious content
                var maliciousValidationResult = ValidateActivityDataForMaliciousContent(activityData);
                if (!maliciousValidationResult.IsValid)
                {
                    return CreateErrorResponse(maliciousValidationResult.ErrorMessage, "400");
                }

                await _newDealerRepo.UpdateClaimRepo(claimId, activityData, empNo);
                return CreateSuccessResponse(claimId, "Claim Updated successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error Updating Claim", "500", ex.Message);
            }
        }

        /// <summary>
        /// Approves or rejects a claim based on the provided filter criteria.
        /// </summary>
        /// <param name="filter">Filter containing approval/rejection details</param>
        /// <returns>Service response confirming the action</returns>
        public async Task<ServiceResponse> ApproveRejectClaimService(FilterModel filter)
        {
            try
            {
                if (filter == null)
                    throw new ArgumentNullException(nameof(filter), "Filter cannot be null");

                // Validate reject remarks for malicious content
                if (!string.IsNullOrEmpty(filter.RejectRemarks))
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(filter.RejectRemarks))
                    {
                        return CreateErrorResponse("Reject Remarks contains potentially malicious content", "400");
                    }
                }

                var claimId = await _newDealerRepo.ApproveRejectClaimRepo(filter);
                return CreateSuccessResponse(claimId, "Claim Approved/Rejected Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error Approving/Rejecting Claim", "500", ex.Message);
            }
        }

        /// <summary>
        /// Retrieves detailed information for a specific claim.
        /// </summary>
        /// <param name="claimId">The claim ID to retrieve details for</param>
        /// <returns>Service response containing claim details</returns>
        public async Task<ServiceResponse> ClaimDetailsService(int claimId)
        {
            try
            {
                if (claimId <= 0)
                    throw new ArgumentException("Claim ID must be greater than zero", nameof(claimId));

                var claimDetails = await _newDealerRepo.ClaimDetailsRepo(claimId);
                return CreateSuccessResponse(claimDetails, "Claim Details retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error retrieving Claim Details", "500", ex.Message);
            }
        }

        #endregion

        #region Actual Claim Services

        /// <summary>
        /// Adds a new actual claim after validating the request data.
        /// </summary>
        /// <param name="request">The actual claim request data</param>
        /// <returns>Service response containing the claim ID</returns>
        public async Task<ServiceResponse> AddActualClaimService(ActualClaimModel request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Actual claim request cannot be null");

                // Validate request data for malicious content
                var validationResult = ValidateActualClaimForMaliciousContent(request);
                if (!validationResult.IsValid)
                {
                    return CreateErrorResponse(validationResult.ErrorMessage, "400");
                }

                var claimId = await _newDealerRepo.AddActualClaimRepo(request);
                return CreateSuccessResponse(claimId, "Claim Submitted successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error submitting Claim", "500", ex.Message);
            }
        }

        public async Task<ServiceResponse> UpdateActualClaimService(ActualClaimUpdateModel request)
        {
            try
            {
                if (request == null)
                    throw new ArgumentNullException(nameof(request), "Actual claim update request cannot be null");

                if (request.ClaimId <= 0)
                    throw new ArgumentException("ClaimId must be greater than 0", nameof(request.ClaimId));

                // Validate request data for malicious content (only non-null fields)
                var validationResult = ValidateActualClaimUpdateForMaliciousContent(request);
                if (!validationResult.IsValid)
                {
                    return CreateErrorResponse(validationResult.ErrorMessage, "400");
                }

                var updatedClaimId = await _newDealerRepo.UpdateActualClaimRepo(request);

                if (updatedClaimId == 0)
                {
                    return CreateErrorResponse("Claim not found or no changes made", "404");
                }

                return CreateSuccessResponse(updatedClaimId, "Claim updated successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error updating Claim", "500", ex.Message);
            }
        }


        /// <summary>
        /// Adds actual remarks to a claim after validation.
        /// </summary>
        /// <param name="claimId">The claim ID to add remarks to</param>
        /// <param name="actualRemarks">The remarks to add</param>
        /// <returns>Service response containing the activity ID</returns>
        public async Task<ServiceResponse> AddActualRemarksService(int claimId, string actualRemarks)
        {
            try
            {
                if (claimId <= 0)
                    throw new ArgumentException("Claim ID must be greater than zero", nameof(claimId));

                // Validate remarks for malicious content
                if (!string.IsNullOrEmpty(actualRemarks))
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(actualRemarks))
                    {
                        return CreateErrorResponse("Actual Remarks contains potentially malicious content", "400");
                    }
                }

                var activityId = await _newDealerRepo.AddActualRemarkRepo(claimId, actualRemarks);
                return CreateSuccessResponse(activityId, "Actual Remarks Added successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error adding Actual Remarks", "500", ex.Message);
            }
        }



        /// <summary>
        /// Retrieves detailed information for a specific actual claim.
        /// </summary>
        /// <param name="activityId">The activity ID to retrieve details for</param>
        /// <returns>Service response containing actual claim details</returns>
        public async Task<ServiceResponse> ActualClaimDetailsService(int activityId)
        {
            try
            {
                if (activityId <= 0)
                    throw new ArgumentException("Activity ID must be greater than zero", nameof(activityId));

                var actualClaimDetails = await _newDealerRepo.ActualClaimDetailsRepo(activityId);
                return CreateSuccessResponse(actualClaimDetails, "Actual Claim Details retrieved successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error retrieving Actual Claim Details", "500", ex.Message);
            }
        }

        /// <summary>
        /// Retrieves a list of actual claims based on filter criteria.
        /// </summary>
        /// <param name="filter">Filter criteria for actual claims</param>
        /// <returns>Service response containing the list of actual claims</returns>
        public async Task<ServiceResponse> ActualClaimListService(FilterModel filter)
        {
            try
            {
                var claimDetails = await _newDealerRepo.ActualClaimListRepo(filter);
                return CreateSuccessResponse(claimDetails, "Actual Claim List retrieved successfully");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error retrieving Actual Claim List", "500", ex.Message);
            }
        }

        /// <summary>
        /// Approves or rejects an actual claim based on the provided filter criteria.
        /// </summary>
        /// <param name="filter">Filter containing approval/rejection details</param>
        /// <returns>Service response confirming the action</returns>
        public async Task<ServiceResponse> ApproveRejectActualClaimService(FilterModel filter)
        {
            try
            {
                if (filter == null)
                    throw new ArgumentNullException(nameof(filter), "Filter cannot be null");

                // Validate reject remarks for malicious content
                if (!string.IsNullOrEmpty(filter.RejectRemarks))
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(filter.RejectRemarks))
                    {
                        return CreateErrorResponse("Reject Remarks contains potentially malicious content", "400");
                    }
                }

                var claimId = await _newDealerRepo.ApproveRejectActualClaimRepo(filter);
                return CreateSuccessResponse(claimId, "Actual Claim Approved/Rejected Successfully");
            }
            catch (ArgumentNullException ex)
            {
                return CreateErrorResponse(ex.Message, "400");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse("Error Approving/Rejecting Actual Claim", "500", ex.Message);
            }
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Validates pagination parameters to ensure they are within acceptable ranges.
        /// </summary>
        /// <param name="pageIndex">The page index to validate</param>
        /// <param name="pageSize">The page size to validate</param>
        /// <exception cref="ArgumentException">Thrown when parameters are invalid</exception>
        private static void ValidatePaginationParameters(int pageIndex, int pageSize)
        {
            if (pageIndex < 0)
                throw new ArgumentException("Page index cannot be negative", nameof(pageIndex));

            if (pageSize <= 0 || pageSize > 1000)
                throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));
        }

        /// <summary>
        /// Validates claim submission parameters for completeness and correctness.
        /// </summary>
        /// <param name="requestNo">Request number</param>
        /// <param name="dealerNo">Dealer number</param>
        /// <param name="activityData">Activity data list</param>
        /// <param name="empNo">Employee number</param>
        /// <returns>Validation result indicating success or failure</returns>
        private static ValidationResult ValidateClaimSubmissionParameters(string requestNo, string dealerNo, List<ActivityModel> activityData, string empNo)
        {
            if (string.IsNullOrWhiteSpace(requestNo))
                return ValidationResult.Failed("Request number cannot be null or empty");

            if (string.IsNullOrWhiteSpace(dealerNo))
                return ValidationResult.Failed("Dealer number cannot be null or empty");

            if (string.IsNullOrWhiteSpace(empNo))
                return ValidationResult.Failed("Employee number cannot be null or empty");

            if (activityData == null || !activityData.Any())
                return ValidationResult.Failed("Activity data cannot be empty");

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates activity data for malicious content patterns.
        /// </summary>
        /// <param name="activityData">List of activity data to validate</param>
        /// <returns>Validation result indicating success or failure</returns>
        private ValidationResult ValidateActivityDataForMaliciousContent(List<ActivityModel> activityData)
        {
            foreach (var activity in activityData)
            {
                var propertiesToValidate = new Dictionary<string, string>
                {
                    { nameof(activity.ActivityType), activity.ActivityType },
                    { nameof(activity.ActivityThrough), activity.ActivityThrough },
                    { nameof(activity.ActivityMonth), activity.ActivityMonth },
                    { nameof(activity.BudgetRequested), activity.BudgetRequested }
                };

                foreach (var prop in propertiesToValidate)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(prop.Value))
                    {
                        string displayName = FormatPropertyName(prop.Key);
                        return ValidationResult.Failed($"{displayName} contains potentially malicious content");
                    }
                }
            }
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates actual claim data for malicious content patterns.
        /// </summary>
        /// <param name="request">Actual claim request to validate</param>
        /// <returns>Validation result indicating success or failure</returns>
        private ValidationResult ValidateActualClaimForMaliciousContent(ActualClaimModel request)
        {
            var fieldsToValidate = new Dictionary<string, string>
            {
                { nameof(request.ActualExpenses), request.ActualExpenses },
                { nameof(request.DateOfActivity), request.DateOfActivity },
                { nameof(request.CustomerContacted), request.CustomerContacted },
                { nameof(request.Enquiry), request.Enquiry },
                { nameof(request.Delivery), request.Delivery }
            };

            foreach (var field in fieldsToValidate)
            {
                if (_fileValidationService.ContainsMaliciousPatterns(field.Value))
                {
                    string displayName = FormatPropertyName(field.Key);
                    return ValidationResult.Failed($"{displayName} contains potentially malicious content");
                }
            }
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates actual claim data for malicious content patterns.
        /// </summary>
        /// <param name="request">Actual claim request to validate</param>
        /// <returns>Validation result indicating success or failure</returns>
        private ValidationResult ValidateActualClaimUpdateForMaliciousContent(ActualClaimUpdateModel request)
        {
            var fieldsToValidate = new Dictionary<string, string>
            {
                { nameof(request.ActualExpenses), request.ActualExpenses },
                { nameof(request.DateOfActivity), request.DateOfActivity },
                { nameof(request.CustomerContacted), request.CustomerContacted },
                { nameof(request.Enquiry), request.Enquiry },
                { nameof(request.Delivery), request.Delivery }
            };

            foreach (var field in fieldsToValidate)
            {
                if (_fileValidationService.ContainsMaliciousPatterns(field.Value))
                {
                    string displayName = FormatPropertyName(field.Key);
                    return ValidationResult.Failed($"{displayName} contains potentially malicious content");
                }
            }
            return ValidationResult.Success();
        }



        /// <summary>
        /// Formats property names for display in error messages by adding spaces before capital letters.
        /// </summary>
        /// <param name="propertyName">The property name to format</param>
        /// <returns>Formatted property name</returns>
        private static string FormatPropertyName(string propertyName)
        {
            return string.Concat(propertyName.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Trim();
        }

        /// <summary>
        /// Creates a successful service response with result and total count.
        /// </summary>
        /// <param name="result">The result data</param>
        /// <param name="totalCount">Total count for pagination</param>
        /// <param name="message">Success message</param>
        /// <returns>Service response indicating success</returns>
        private static ServiceResponse CreateSuccessResponse(object result, int totalCount, string message)
        {
            return new ServiceResponse
            {
                isError = false,
                result = result,
                totalCount = totalCount,
                Message = message,
                Code = "200",
                Status = "Success"
            };
        }

        /// <summary>
        /// Creates a successful service response with result only.
        /// </summary>
        /// <param name="result">The result data</param>
        /// <param name="message">Success message</param>
        /// <returns>Service response indicating success</returns>
        private static ServiceResponse CreateSuccessResponse(object result, string message)
        {
            return new ServiceResponse
            {
                isError = false,
                result = result,
                Message = message,
                Code = "200",
                Status = "Success"
            };
        }

        /// <summary>
        /// Creates an error service response.
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="code">Error code</param>
        /// <param name="error">Detailed error information</param>
        /// <returns>Service response indicating error</returns>
        private static ServiceResponse CreateErrorResponse(string message, string code, string error = null)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = error,
                Message = message,
                Status = "Error",
                Code = code
            };
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// Represents the result of a validation operation.
        /// </summary>
        private class ValidationResult
        {
            public bool IsValid { get; private set; }
            public string ErrorMessage { get; private set; }

            private ValidationResult(bool isValid, string errorMessage = null)
            {
                IsValid = isValid;
                ErrorMessage = errorMessage;
            }

            public static ValidationResult Success() => new(true);
            public static ValidationResult Failed(string errorMessage) => new(false, errorMessage);
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