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
    public interface IRequestRepository
    {
        Task<List<RequestTypeFilterModel>> RequestTypeFilterRepo();
        Task<List<HPCategoryFilterModel>> HPCategoryFilterRepo();
        Task<(List<RequestModel> requests, int TotalCount)> RequestListRepo(FilterModel filter, int pageIndex, int pageSize);
        Task<RequestSubmissionResult> SubmitRequestAsync(RequestSubmissionModel request, string empNo,string roleId);
    }
}
