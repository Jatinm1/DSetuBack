using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class PerformanceSheetModel
    {
        public int Id { get; set; }
        public int DealerEmpId { get; set; }
        public string TractorVol { get; set; }
        public string SpareParts { get; set; }
        public string XMOil { get; set; }
        public string TractorVolAdherence { get; set; }
        public string SparePartsAdherence { get; set; }
        public string XMOilAdherence { get; set; }
        public string BusinessPerformanceRemarks { get; set; }
        public string OwnFundPlan { get; set; }
        public string BGPlan { get; set; }
        public string OwnFund { get; set; }
        public string BG { get; set; }
        public string TACFLPlan { get; set; }
        public string TACFLActual { get; set; }
        public string FundRemarks { get; set; }
        public string SalesManpower { get; set; }
        public string ServiceManpower { get; set; }
        public string AdminManpower { get; set; }
        public string SalesBranch { get; set; }
        public string ServiceBranch { get; set; }
        public string AdminBranch { get; set; }
        public string SalesInfra { get; set; }
        public string ServiceInfra { get; set; }
        public string AdminInfra { get; set; }
        public string SalesCIP { get; set; }
        public string ServiceCIP { get; set; }
        public string AdminCIP { get; set; }
        public string SalesManpowerAct { get; set; }
        public string ServiceManpowerAct { get; set; }
        public string AdminManpowerAct { get; set; }
        public string SalesBranchAct { get; set; }
        public string ServiceBranchAct { get; set; }
        public string AdminBranchAct { get; set; }
        public string SalesInfraAct { get; set; }
        public string ServiceInfraAct { get; set; }
        public string AdminInfraAct { get; set; }
        public string SalesCIPAct { get; set; }
        public string ServiceCIPAct { get; set; }
        public string AdminCIPAct { get; set; }
        public string CoverageRemarks { get; set; }
        public string ShowroomSize { get; set; }
        public string WorkshopSize { get; set; }
        public string OwnRentedPlan { get; set; }
        public string OwnRentedActual { get; set; }
        public string CIPStatusPlan { get; set; }
        public string CIPStatusActual { get; set; }
        public string CoverageRemarksPlan { get; set; }
        public string CoverageRemarksActual { get; set; }
        public string FieldActvities { get; set; }
        public string FinalRemarks { get; set; }
        public int IsActionPlanReq { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public int Month { get; set; }
        public string FYear { get; set; }
        public string ActionRequired { get; set; }
        public string Remarks { get; set; }

        public List<FieldActivity> fieldActivities { get; set; }
    }
    public class FieldActivity
    {
        public string Quater { get; set; }
        public string SalesPlan { get; set; }
        public string ServicePlan { get; set; }
        public string OthersPlan { get; set; }
        public string SalesActual { get; set; }
        public string ServiceActual { get; set; }
        public string OthersActual { get; set; }
    }
}
