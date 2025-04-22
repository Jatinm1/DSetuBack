using DealerSetu_Repositories.IRepositories;
using Microsoft.Extensions.Logging;
using DealerSetu_Services.IServices;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Services.Services
{
    public class RequestService : IRequestService
    {
        private readonly IRequestRepository _requestRepository;
        //private readonly IEmailService _emailService;
        private readonly ILogger<RequestService> _logger;

        public RequestService(
            IRequestRepository requestRepository,
            //IEmailService emailService,
            ILogger<RequestService> logger)
        {
            _requestRepository = requestRepository;
            //_emailService = emailService;
            _logger = logger;
        }

        public async Task<ServiceResponse> RequestTypeFilterService()
        {
            try
            {
                var requestTypes = await _requestRepository.RequestTypeFilterRepo();

                return new ServiceResponse
                {
                    isError = false,
                    result = requestTypes,
                    Message = "Request Types retrieved successfully",
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
                    Message = "Error retrieving Request Types",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> HPCategoryService()
        {
            try
            {
                var HPCategories = await _requestRepository.HPCategoryFilterRepo();

                return new ServiceResponse
                {
                    isError = false,
                    result = HPCategories,
                    Message = "HP Categories retrieved successfully",
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
                    Message = "Error retrieving HP Categories",
                    Status = "Error",
                    Code = "500"
                };
            }
        }

        public async Task<ServiceResponse> SubmitRequestService(string requestTypeId, string message, string hpCategory, string empNo)
        {
            try
            {
                // Submit the request and get result and notification data
                var submissionResult = await _requestRepository.SubmitRequestAsync(requestTypeId, message, hpCategory, empNo);

                //if (submissionResult.RequestId > 0)
                //{
                //    // Send email notifications
                //    await SendEmailNotificationsAsync(submissionResult);
                //}

                return new ServiceResponse
                {
                    isError = false,
                    result = $"Your RequestId is : {submissionResult.RequestId}",
                    Message = "Request Added successfully",
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
                    Message = "Error while Submitting Request",
                    Code = "500",
                    Status = "Error"
                };

            }
        }

        public async Task<ServiceResponse> RequestListService(FilterModel filter, int pageIndex, int pageSize)
        {
            try
            {
                var (Requests, totalCount) = await _requestRepository.RequestListRepo(filter, pageIndex, pageSize);

                return new ServiceResponse
                {
                    isError = false,
                    result = Requests,
                    totalCount = totalCount,
                    Message = "Requests retrieved successfully",
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
                    Message = "Error while retrieving Requests",
                    Code = "500",
                    Status = "Error"
                };
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
    }
}
