using DealerSetu_Repositories.IRepositories;
using Microsoft.Extensions.Logging;
using DealerSetu_Services.IServices;
using DealerSetu_Repositories.Repositories;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Services.Services
{
    public class NewDealerActivityService : INewDealerActivityService
    {
        private readonly INewDealerActivityRepository _newDealerRepo;
        //private readonly IEmailService _emailService;
        private readonly ILogger<RequestService> _logger;
        private readonly IBlobStorageService _blobStorageService;
        private readonly IFileValidationService _fileValidationService;

        public NewDealerActivityService(
            INewDealerActivityRepository newDealerRepo,
            //IEmailService emailService,
            ILogger<RequestService> logger,
            IBlobStorageService blobStorageService,
            IFileValidationService fileValidationService)
        {
            _newDealerRepo = newDealerRepo;
            //_emailService = emailService;
            _logger = logger;
            _blobStorageService = blobStorageService;
            _fileValidationService = fileValidationService;
        }

        //****************************************NEW DEALER CLAIM API Services**************************************
        public async Task<ServiceResponse> NewDealerActivityListService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var (newActivities, totalCount) = await _newDealerRepo.NewDealerActivityRepo(filter, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = newActivities,
                    totalCount = totalCount,
                    Message = "New Activities retrieved successfully",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error while retrieving New Activities",
                    Code = "500",
                    Status = "Error"
                };
            }
        }

        public async Task<ServiceResponse> NewDealerPendingListService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var (newPendingActivities, totalCount) = await _newDealerRepo.NewDealerPendingListRepo(filter, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = newPendingActivities,
                    totalCount = totalCount,
                    Message = "New Pending Activities retrieved successfully",
                    Code = "200",
                    Status = "Success"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error while retrieving New Pending Activities",
                    Code = "500",
                    Status = "Error"
                };
            }
        }

        public async Task<ServiceResponse> DealerStatesService()
        {
            try
            {
                var states = await _newDealerRepo.DealerStatesRepo();
                return new ServiceResponse
                {
                    isError = false,
                    result = states,
                    Message = "States retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving states",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> DealerDataService(string requestNo)
        {
            try
            {
                var dealerData = await _newDealerRepo.DealerDataRepo(requestNo);
                return new ServiceResponse
                {
                    isError = false,
                    result = dealerData,
                    Message = "Dealer Data retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Dealer Data",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> SubmitClaimService(string requestNo, string dealerNo, List<ActivityModel> activityData, string empNo)
        {
            try
            {
                // Validate activityData
                if (activityData == null || !activityData.Any())
                {
                    return new ServiceResponse
                    {
                        isError = true,
                        result = null,
                        Status = "Error",
                        Message = "Activity data cannot be empty",
                        Code = "400"
                    };
                }
                // Validate each activity in the list
                foreach (var activity in activityData)
                {
                    // Define properties to validate for each activity
                    var propertiesToValidate = new Dictionary<string, string>
        {
            { nameof(activity.ActivityType), activity.ActivityType },
            { nameof(activity.ActivityThrough), activity.ActivityThrough },
            { nameof(activity.ActivityMonth), activity.ActivityMonth },
            { nameof(activity.BudgetRequested), activity.BudgetRequested}
        };
                    // Check each property for validation
                    foreach (var prop in propertiesToValidate)
                    {
                        string propName = prop.Key;
                        string propValue = prop.Value;
                        // Format property name for display in error message
                        string displayName = string.Concat(propName.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Trim();
                        // Check for malicious patterns
                        if (_fileValidationService.ContainsMaliciousPatterns(propValue))
                        {
                            return new ServiceResponse
                            {
                                isError = true,
                                result = null,
                                Status = "Error",
                                Message = $"{displayName} contains potentially malicious content",
                                Code = "400"
                            };
                        }
                    }
                }

                var claimId = await _newDealerRepo.SubmitClaimRepo(requestNo, dealerNo, activityData, empNo); ;
                return new ServiceResponse
                {
                    isError = false,
                    result = claimId,
                    Message = "Claim Submitted successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error submitting Claim",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> UpdateClaimService(int claimId, List<ActivityModel> activityData, string empNo)
        {
            try
            {
                var claimIdS = await _newDealerRepo.UpdateClaimRepo(claimId, activityData, empNo); ;
                return new ServiceResponse
                {
                    isError = false,
                    result = claimId,
                    Message = "Claim Updated successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error Updating Claim",
                    Status = "Error",
                    Code = "500"
                };
            }
        }
        
        public async Task<ServiceResponse> ApproveRejectClaimService(FilterModel filter)
        {
            try
            {
                if (filter.RejectRemarks.Any() || filter.RejectRemarks != null)
                {

                    if (_fileValidationService.ContainsMaliciousPatterns(filter.RejectRemarks))
                        return new ServiceResponse
                        {
                            isError = true,
                            result = null,
                            Status = "Error",
                            Message = "Reject Remarks contains potentially malicious content",
                            Code = "400"
                        };
                }
                var claimId = await _newDealerRepo.ApproveRejectClaimRepo(filter);
                return new ServiceResponse
                {
                    isError = false,
                    result = claimId,
                    Message = "Claim Approved/Rejected Successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error Approving/Rejecting Claim",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> ClaimDetailsService(int claimId)
        {
            try
            {
                var claimDetails = await _newDealerRepo.ClaimDetailsRepo(claimId);
                return new ServiceResponse
                {
                    isError = false,
                    result = claimDetails,
                    Message = "Claim Details retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Claim Details",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        //***********************************NEW DEALER ACTUAL CLAIM API Services************************************

        public async Task<ServiceResponse> AddActualClaimService(ActualClaimModel request)
        {
            try
            {
                // Define fields to validate
                var fieldsToValidate = new Dictionary<string, string>
                    {
                        { nameof(request.ActualExpenses), request.ActualExpenses },
                        { nameof(request.DateOfActivity), request.DateOfActivity },
                        { nameof(request.CustomerContacted), request.CustomerContacted },
                        { nameof(request.Enquiry), request.Enquiry },
                        { nameof(request.Delivery), request.Delivery }
                    };
                // Check each field for validation
                foreach (var field in fieldsToValidate)
                {
                    string fieldName = field.Key;
                    string fieldValue = field.Value;
                    // Format field name for display in error message (add spaces before capital letters)
                    string displayName = string.Concat(fieldName.Select(c => char.IsUpper(c) ? " " + c : c.ToString())).Trim();
                    // Check for malicious patterns
                    if (_fileValidationService.ContainsMaliciousPatterns(fieldValue))
                    {
                        return new ServiceResponse
                        {
                            isError = true,
                            result = null,
                            Status = "Error",
                            Message = $"{displayName} contains potentially malicious content",
                            Code = "400"
                        };
                    }
                }
                var claimId = await _newDealerRepo.AddActualClaimRepo(request); ;
                return new ServiceResponse
                {
                    isError = false,
                    result = claimId,
                    Message = "Claim Submitted successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error submitting Claim",
                    Status = "Error",
                    Code = "500"
                };
            }
        }
        
        public async Task<ServiceResponse> AddActualRemarksService(int claimId, string actualRemarks)
        {
            try
            {
                if (actualRemarks.Any() || actualRemarks != null)
                { 

                    if (_fileValidationService.ContainsMaliciousPatterns(actualRemarks))
                        return new ServiceResponse
                        {
                            isError = true,
                            result = null,
                            Status = "Error",
                            Message = "Actual Remarks contains potentially malicious content",
                            Code = "400"
                        };
            
                }
                var activityId = await _newDealerRepo.AddActualRemarkRepo(claimId, actualRemarks);
                return new ServiceResponse
                {
                    isError = false,
                    result = activityId,
                    Message = "Actual Remarks Added successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error adding Actual Remarks",
                    Status = "Error",
                    Code = "500"
                };
            }
        }
        
        public async Task<ServiceResponse> ActualClaimDetailsService(int activityId)
        {
            try
            {
                ActualClaimModel actualclaimDetails = await _newDealerRepo.ActualClaimDetailsRepo(activityId);
                
                return new ServiceResponse
                {
                    isError = false,
                    result = actualclaimDetails,
                    Message = "Actual Claim Details retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Actual Claim Details",
                    Status = "Error",
                    Code = "500"
                };
            }            
        }

        public async Task<ServiceResponse> ActualClaimListService(FilterModel filter)
        {
            try
            {
                var claimDetails = await _newDealerRepo.ActualClaimListRepo(filter);
                return new ServiceResponse
                {
                    isError = false,
                    result = claimDetails,
                    Message = "Actual Claim List retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Actual Claim List",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> ApproveRejectActualClaimService(FilterModel filter)
        {
            try
            {
                if (filter.RejectRemarks != null)
                {
                    if (_fileValidationService.ContainsMaliciousPatterns(filter.RejectRemarks))
                        return new ServiceResponse
                        {
                            isError = true,
                            result = null,
                            Status = "Error",
                            Message = "Reject Remarks contains potentially malicious content",
                            Code = "400"
                        };
                }
                var claimId = await _newDealerRepo.ApproveRejectActualClaimRepo(filter);
                return new ServiceResponse
                {
                    isError = false,
                    result = claimId,
                    Message = "Actual Claim Approved/Rejected Successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error Approving/Rejecting Actual Claim",
                    Status = "Error",
                    Code = "500"
                };
            }
        }
        
        private ServiceResponse CreateResponse(bool isError, string message, string code)
        {
            return new ServiceResponse
            {
                isError = isError,
                Message = message,
                Status = isError ? "Failure" : "Success",
                Code = code
            };
        }
        
        private ServiceResponse CreateErrorResponse(Exception ex, string message)
        {
            return new ServiceResponse
            {
                isError = true,
                Error = ex.Message,
                Message = message,
                Status = "Error",
                Code = "500"
            };
        }
        private UpdateResult CreateUpdateErrorResult(string message)
        {
            return new UpdateResult
            {
                RowsAffected = 0,
                Message = message,
                IsSuccess = false
            };
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
    }
}
