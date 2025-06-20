using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Services.IServices
{
    public interface IPerformanceSheetService
    {
        Task<ServiceResponse> GetTrackingDealersServiceAsync(DealersRequestModel request, string empNo);
        Task<ServiceResponse> GetPendingDealersServiceAsync(DealersRequestModel request, string empNo);
        Task<ServiceResponse> GetDealerListServiceAsync(string empNo);
        Task<ServiceResponse> GetPerformanceSheetServiceAsync(PerformanceSheetReqModel request);
        Task<ServiceResponse> GetDealerBusinessPlanServiceAsync(BusinessPlanReqModel request);
        Task<ServiceResponse> GetDealerDetailsServiceAsync(PerformanceSheetReqModel request);
        Task<ServiceResponse> GetActionPlanDetailServiceAsync(ActionPlanDetailReqModel request);
        Task<ServiceResponse> SubmitPerformanceSheetServiceAsync(PerformanceSheetUpdateModel request, string empNo);
        Task<ServiceResponse> SubmitActionPlanServiceAsync(ActionPlanModel request);
        Task<ServiceResponse> SubmitDealerBusinessPlanServiceAsync(BusinessPerformancePlan request);

    }
}
