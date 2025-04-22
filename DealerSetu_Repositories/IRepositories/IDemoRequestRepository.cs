using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.IRepositories
{
    public interface IDemoRequestRepository
    {
        Task<(List<DemoTractorResponseModel> DemoTractorList, int TotalCount)> DemoTractorApprovedRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<(List<DemoTractorResponseModel> PendingDemoTractorList, int TotalCount)> DemoTractorPendingRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<List<FYearModel>> FiscalYearsRepo();
        Task<int> SubmitDemoReqRepo(DemoReqSubmissionModel request, string empNo);
        Task<DemoReqModel> DemoReqDataRepo(int reqId);
        Task<int> DemoTractorApproveRejectRepo(FilterModel filter);
        Task<List<DemoReqModel>> DemoActualClaimListRepo(FilterModel filter);
        //Task<int> AddDemoActualClaimRepo(DemoReqModel docModel);
        Task<int> AddBasicDemoActualClaimRepo(DemoReqModel docModel);
        Task<int> AddAllDemoActualClaimRepo(DemoReqModel docModel);
        Task<List<DemoReqModel>> GetDemoTractorDoc(FilterModel filter);

    }
}
