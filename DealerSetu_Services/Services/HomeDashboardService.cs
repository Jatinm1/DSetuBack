using DealerSetu_Data.Models.HelperModels;
using DealerSetu_Data.Models.ViewModels;
using DealerSetu_Repositories.IRepositories;
using DealerSetu_Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class HomeDashboardService : IHomeDashboardService
    {
        private readonly IHomeDashboardRepo _homeRepo;

        public HomeDashboardService(IHomeDashboardRepo repository)
        {
            _homeRepo = repository;
        }

        //public async Task<HomeDashboard> GetDashboardDataAsync(string userId)
        //{
        //    var dashboard = await _repository.GetUserDashboardDataAsync(userId);
        //    if (dashboard == null)
        //        throw new Exception("User not found");

        //    //dashboard.Counts = await _repository.GetDashboardCountsAsync(userId);
        //    dashboard.MenuItems = GetMenuItems(dashboard);

        //    return dashboard;
        //}

        public async Task<ServiceResponse> PendingCountService(FilterModel filter)
        {
            try
            {
                var PendingCounts = await _homeRepo.PendingCountRepo(filter.EmpNo, filter.RoleId);

                return new ServiceResponse
                {
                    isError = false,
                    result = PendingCounts,
                    Message = "Pending Counts retrieved successfully",
                    Status = "Success",
                    Code = "200"
                };
            }
            catch (Exception ex)
            {
                return new ServiceResponse
                {
                    isError = true,
                    Error = ex.Message,
                    Message = "Error retrieving Pending",
                    Status = "Error",
                    Code = "500"
                };
            }
        }
    }
}
