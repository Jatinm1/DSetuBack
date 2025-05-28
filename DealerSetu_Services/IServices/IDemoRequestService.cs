using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IDemoRequestService
    {
        Task<ServiceResponse> DemoTractorApprovedService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> DemoTractorPendingService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> DemoTractorPendingClaimService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> FiscalYearsService();
        Task<ServiceResponse> SubmitDemoReqService(DemoReqSubmissionModel request, string empNo);
        Task<ServiceResponse> DemoReqDataService(int reqId);
        Task<ServiceResponse> DemoTractorApproveRejectService(FilterModel filter);
        Task<ServiceResponse> UpdateDemoReqService(DemoReqUpdateModel request, string empNo);
        Task<ServiceResponse> DemoActualClaimListService(FilterModel filter);
        Task<ServiceResponse> AddDemoActualClaimService(DemoReqModel request);
        Task<ServiceResponse> GetDemoTractorDocService(FilterModel filter);
        Task<ServiceResponse> DemoTractorApproveRejectClaimService(FilterModel filter);
        Task<ServiceResponse> AddDemoRemarksService(AddDemoTracRemarksModel request);

    }
}
