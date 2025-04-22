using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.IRepositories
{
    public interface IReportRepository
    {

        Task<(List<ReportModel> Reports, int TotalCount)> RequestSectionReportRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<(List<DemoListModel> DemoRequests, int TotalCount)> DemoTractorReportRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<DemoTractor> RejectedRequestReportRepo(FilterModel filter);
        Task<List<DealerstateModel>> NewDealerStatewiseReportRepo(int fy);
        Task<(List<NewDealerActivity> NewDealerActivities, int TotalCount)> NewDealerActivityReportRepo(FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize);
        Task<(List<NewDealerActivityClaim> NewDealerClaimActivities, int TotalCount)> NewDealerClaimReportRepo(FilterModel filter, int pageIndex, int pageSize);
    }
}
