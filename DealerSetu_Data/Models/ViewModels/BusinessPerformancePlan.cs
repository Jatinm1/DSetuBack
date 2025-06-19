using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class BusinessPerformancePlan
    {

        public int DealerEmpId { get; set; }
        public string FYear { get; set; }
        public string? CreatedBy { get; set; }
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

    public class BusinessPlanResult
    {
        public bool BusinessPlanAdded { get; set; }
    }
}
