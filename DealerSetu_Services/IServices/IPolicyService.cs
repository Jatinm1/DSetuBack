using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Services.IServices
{
    public interface IPolicyService
    {
        ServiceResponse GetPolicyListService();
        public Task<ServiceResponse> SendFiletoServerService(PolicyUploadModel request, int RAId);

    }
}





//*************************************ADD THESE ABOVE FOR UPLOADING FILE*************************************

//ServiceResponse GetWhiteListingService();
