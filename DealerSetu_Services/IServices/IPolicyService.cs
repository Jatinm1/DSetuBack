using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Services.IServices
{
    public interface IPolicyService
    {
        ServiceResponse GetPolicyListService();
        public Task<ServiceResponse> SendFiletoServerService(FileUploadModel request);
        Task<ServiceResponse> SendPolicytoServerService(PolicyUploadModel request);

    }
}





//*************************************ADD THESE ABOVE FOR UPLOADING FILE*************************************

//ServiceResponse GetWhiteListingService();
