using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface INewDealerActivityService
    {
        //****************************************NEW DEALER CLAIM API SERVICES Interface Methods**************************************

        Task<ServiceResponse> NewDealerActivityListService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> NewDealerPendingListService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> DealerStatesService();
        Task<ServiceResponse> DealerDataService(string requestNo);
        Task<ServiceResponse> SubmitClaimService(string requestNo, string dealerNo, List<ActivityModel> activityData, string empNo);
        Task<ServiceResponse> UpdateClaimService(int claimId, List<ActivityModel> activityData, string empNo);
        Task<ServiceResponse> ClaimDetailsService(int claimId);
        Task<ServiceResponse> ApproveRejectClaimService(FilterModel filter);

        //**********************************NEW DEALER ACTUAL CLAIM API SERVICES Interface Methods**************************************

        Task<ServiceResponse> ActualClaimDetailsService(int activityId);
        Task<ServiceResponse> ApproveRejectActualClaimService(FilterModel filter);
        Task<ServiceResponse> ActualClaimListService(FilterModel filter);
        Task<ServiceResponse> AddActualClaimService(ActualClaimModel request);
        Task<ServiceResponse> UpdateActualClaimService(ActualClaimUpdateModel request);
        Task<ServiceResponse> AddActualRemarksService(int claimId, string actualRemarks);

    }
}
