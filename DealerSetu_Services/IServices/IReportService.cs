using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IReportService
    {
        Task<ServiceResponse> RequestSectionReportService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> DemoTractorReportService(FilterModel filter, int pageIndex, int pageSize);
        Task<DemoTractor> RejectedRequestReportService(FilterModel filter);
        Task<List<DealerstateModel>> NewDealerStatewiseReportService(int fy);
        Task<ServiceResponse> NewDealerActivityReportService(FilterModel filter, bool? pendingByHO, int pageIndex, int pageSize);
        Task<ServiceResponse> NewDealerClaimReportService(FilterModel filter, int pageIndex, int pageSize);

    }
}
