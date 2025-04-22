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




    //    private List<DashboardMenuItem> GetMenuItems(HomeDashboard user)
    //    {
    //        // Query the database to check if the user is associated with Arohan or Abhar
    //        var isArohan = _repository.IsUserInArohanDealer(user.UserId);
    //        var isAbhar = _repository.IsUserInAbharDealer(user.UserId);

    //        var menuItems = new List<DashboardMenuItem>
    //{
    //    new DashboardMenuItem
    //    {
    //        Id = "policy",
    //        Name = "POLICY SECTION",
    //        Icon = "privacy-policy.png",
    //        Url = "/api/policy",
    //        IsVisible = user.RoleId != 9
    //    },
    //    new DashboardMenuItem
    //    {
    //        Id = "request",
    //        Name = "REQUEST SECTION",
    //        Icon = "request.png",
    //        Url = "/api/request",
    //        IsVisible = user.RoleId == 1 || user.RoleId == 6
    //    },
    //    new DashboardMenuItem
    //    {
    //        Id = "village",
    //        Name = "VILLAGE SECTION",
    //        Icon = "village.png",
    //        Url = "/api/village",
    //        IsVisible = user.RoleId is 2 or 3 or 4 or 5 or 6 or 7
    //    },
    //    new DashboardMenuItem
    //    {
    //        Id = "arohan",
    //        Name = "AROHAN SECTION",
    //        Icon = "arohan.png",
    //        Url = "/api/arohan",
    //        IsVisible = user.RoleId == 1 && isArohan
    //    },
    //    new DashboardMenuItem
    //    {
    //        Id = "abhar",
    //        Name = "ABHAR SECTION",
    //        Icon = "abhar.png",
    //        Url = "/api/abhar",
    //        IsVisible = user.RoleId == 1 && isAbhar
    //    },
    //    new DashboardMenuItem
    //    {
    //        Id = "demo",
    //        Name = "DEMO SECTION",
    //        Icon = "demo.png",
    //        Url = "/api/demo",
    //        IsVisible = user.RoleId != 3 && user.RoleId != 4
    //    },
    //    new DashboardMenuItem
    //    {
    //        Id = "claim",
    //        Name = "CLAIM SECTION",
    //        Icon = "claim.png",
    //        Url = user.RoleId == 7 ? "/Claim" : "/Claim/Pending",
    //        IsVisible = true // Always visible
    //    }
    //};

    //        return menuItems.Where(m => m.IsVisible).ToList();
    //    }

    }    }
