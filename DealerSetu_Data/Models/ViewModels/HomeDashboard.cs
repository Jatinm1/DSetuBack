using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class HomeDashboard
    {
        public string UserId { get; set; }
        public string EmpNo { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public int RoleId { get; set; }
        public bool IsArohan { get; set; }
        public bool IsAbhar { get; set; }
        public DashboardCounts Counts { get; set; }
        public List<DashboardMenuItem> MenuItems { get; set; }
    }


    public class DashboardCounts
    {
        public int RequestCount { get; set; }
        public int NewDealerCount { get; set; }
        public int DemoCount { get; set; }
        public int PerformanceCount { get; set; }
    }



    public class DashboardMenuItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Url { get; set; }
        public bool IsVisible { get; set; }
    }

}
