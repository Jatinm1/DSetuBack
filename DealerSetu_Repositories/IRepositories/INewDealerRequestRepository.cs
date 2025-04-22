using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.IRepositories
{
    public interface INewDealerActivityRepository
    {
        //****************************************NEW DEALER CLAIM API Repository Interface Methods**************************************

        Task<(List<ClaimModel> NewDealerActivityList, int TotalCount)> NewDealerActivityRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<(List<ClaimModel> NewDealerPendingList, int TotalCount)> NewDealerPendingListRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<List<DealerStateModel>> DealerStatesRepo();
        Task<UserModel> DealerDataRepo(string requestNo);
        Task<int> SubmitClaimRepo(string requestNo, string dealerNo, List<ActivityModel> activityData, string empNo);
        Task<int> UpdateClaimRepo(int claimId, List<ActivityModel> activityData, string empNo);
        Task<ClaimModel> ClaimDetailsRepo(int claimId);
        Task<int> ApproveRejectClaimRepo(FilterModel filter);

        //**********************************NEW DEALER ACTUAL CLAIM API Repository Interface Methods*************************************

        Task<int> ApproveRejectActualClaimRepo(FilterModel filter);
        Task<List<ActualClaimModel>> ActualClaimListRepo(FilterModel filter);
        Task<int> AddActualClaimRepo(ActualClaimModel actualClaim);
        Task<ActualClaimModel> ActualClaimDetailsRepo(int activityId);
        Task<int> AddActualRemarkRepo(int claimId, string remarks);

    }
}
