using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.IServices
{
    public interface IHomeDashboardService
    {
        Task<ServiceResponse> PendingCountService(FilterModel filter);
        //Task<HomeDashboard> GetDashboardDataAsync(string userId);
    }
}
