using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Repositories.IRepositories
{
    public interface IPerformanceSheetRepo
    {
        Task<(List<DealerModel>, int TotalCount)> GetTrackingDealersRepoAsync(DealersRequestModel request, string empNo);
        Task<(List<DealerModel>, int TotalCount)> GetPendingDealersRepoAsync(DealersRequestModel request, string empNo);
        Task<(List<DealerListModel>, int TotalCount)> GetDealerListRepoAsync(string empNo);
        Task<PerformanceSheetModel> GetPerformanceSheetRepoAsync(PerformanceSheetReqModel request);
        Task<BusinessPerformancePlan> GetDealerBusinessPlanRepoAsync(PerformanceSheetReqModel request);
        Task<BusinessPlanResult> SubmitDealerBusinessPlanRepoAsync(BusinessPerformancePlan planModel);
        Task<DealerModel> GetDealerDetailsRepoAsync(PerformanceSheetReqModel request);
        //Task<ActionPlanModel> GetActionPlanDetailRepoAsync(ActionPlanDetailReqModel request);
        Task<ActionPlanModel> GetActionPlanDetailsRepoAsync(ActionPlanDetailReqModel request);
        Task<ActionPlanResult> SubmitActionPlanRepoAsync(ActionPlanModel actionModel);
        Task<int> SubmitPerformanceSheetRepoAsync(PerformanceSheetModel model);
    }
}
