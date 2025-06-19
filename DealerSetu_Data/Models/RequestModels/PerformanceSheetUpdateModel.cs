using DealerSetu_Data.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class PerformanceSheetUpdateModel
    {
        public int DealerEmpId { get; set; }
        public int Month { get; set; }
        public string FYear { get; set; }
        public string TractorVol { get; set; }
        public string TractorAdherence { get; set; }
        public string BusinessRemarks { get; set; }
        public string SpareParts { get; set; }
        public string SparPartsAdherence { get; set; }
        public string XmOil { get; set; }
        public string XmOilAdherence { get; set; }
        public string TaCFLPlan { get; set; }
        public string OwnFundPlan { get; set; }
        public string BgPlan { get; set; }
        public string OwnFund { get; set; }
        public string Bg { get; set; }
        public string TaCFLActual { get; set; }
        public string FundRemarks { get; set; }
        public string SalesManpower { get; set; }
        public string SalesBranch { get; set; }
        public string SalesInfra { get; set; }
        public string SalesCIP { get; set; }
        public string ServiceManpower { get; set; }
        public string ServiceBranch { get; set; }
        public string ServiceInfra { get; set; }
        public string ServiceCIP { get; set; }
        public string AdminManpower { get; set; }
        public string AdminBranch { get; set; }
        public string AdminInfra { get; set; }
        public string AdminCIP { get; set; }
        public string SalesManpowerAct { get; set; }
        public string SalesBranchAct { get; set; }
        public string SalesInfraAct { get; set; }
        public string SalesCIPAct { get; set; }
        public string ServiceManpowerAct { get; set; }
        public string ServiceBranchAct { get; set; }
        public string ServiceInfraAct { get; set; }
        public string ServiceCIPAct { get; set; }
        public string AdminManpowerAct { get; set; }
        public string AdminBranchAct { get; set; }
        public string AdminInfraAct { get; set; }
        public string AdminCIPAct { get; set; }
        public string CoverageRemarks { get; set; }
        public string OwnRentedPlan { get; set; }
        public string CipStatusPlan { get; set; }
        public string CoverageRemarksPlan { get; set; }
        public string ShowroomSize { get; set; }
        public string WorkshopSize { get; set; }
        public string OwnRentedActual { get; set; }
        public string CipStatusActual { get; set; }
        public string CoverageRemarksActual { get; set; }
        public string FinalRemarks { get; set; }
        public int IsActionPlanReq { get; set; }
        public string ActionRequired { get; set; }
        public List<FieldActivity> FieldActivities { get; set; } = new List<FieldActivity>();
    }
}
