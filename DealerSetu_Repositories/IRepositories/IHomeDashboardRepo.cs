using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Repositories.IRepositories
{
    public interface IHomeDashboardRepo
    {
        Task<HomeDashboard> GetUserDashboardDataAsync(string userId);
        bool IsUserInAbharDealer(string userId);
        bool IsUserInArohanDealer(string userId);
        Task<List<PendingCountModel>> PendingCountRepo(string empNo, string roleId);
        //Task<DashboardCounts> GetDashboardCountsAsync(string userId);
    }
}
