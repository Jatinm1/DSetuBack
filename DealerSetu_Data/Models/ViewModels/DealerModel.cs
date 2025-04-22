using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class DealerModel
    {
        public int UserId { get; set; }
        public string DealerNo { get; set; }
        public string RealDealerNo { get; set; }
        public string DealershipName { get; set; }
        public string Email { get; set; }
        public string State { get; set; }
        public int RoleId { get; set; }
        public string Role { get; set; }
        public string Location { get; set; }
        public string District { get; set; }
        public string Zone { get; set; }
        public string DateOfAppointment { get; set; }
        public string DealershipAge { get; set; }
        public string Industry { get; set; }
        public string TRVolPlan { get; set; }
        public string PlanVol { get; set; }
        public string OwnFund { get; set; }
        public string BGFund { get; set; }
        public string SalesManpower { get; set; }
        public string ServiceManpower { get; set; }
        public string AdminManpower { get; set; }
        public string ShowroomSize { get; set; }
        public string WorkshopSize { get; set; }
        public string TM { get; set; }
        public string SCM { get; set; }
        public string AM { get; set; }
        public string CCM { get; set; }
        public string CM { get; set; }
        public string SH { get; set; }
        public string TractorVol { get; set; }
        public string SpareParts { get; set; }
        public string XMOil { get; set; }
        public string TractorVolActual { get; set; }
        public string SparePartsActual { get; set; }
        public string XMOilActual { get; set; }
        public string Remarks { get; set; }
    }
    public class BusinessPerformancePlan
    {

        public int DealerEmpId { get; set; }
        public string FYear { get; set; }
        public string CreatedBy { get; set; }
        public List<BusinessPerformanceSheet> businessPerformanceSheets { get; set; }

    }
    public class BusinessPerformanceSheet
    {
        public int Month { get; set; }
        public string TractorVol { get; set; }
        public string SpareParts { get; set; }
        public string XMOil { get; set; }
        public string TractorVolActual { get; set; }
        public string SparePartsActual { get; set; }
        public string XMOilActual { get; set; }
        public string Remarks { get; set; }
    }
}
