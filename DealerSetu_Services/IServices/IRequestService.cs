using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.RequestModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IRequestService
    {
        Task<ServiceResponse> RequestTypeFilterService();
        Task<ServiceResponse> HPCategoryService();
        Task<ServiceResponse> RequestListService(FilterModel filter, int pageIndex, int pageSize);
        Task<ServiceResponse> SubmitRequestService(RequestSubmissionModel request, string empNo, string roleId);

    }
}
