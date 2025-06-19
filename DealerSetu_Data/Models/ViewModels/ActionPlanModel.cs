using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class ActionPlanModel
    {
        public int Id { get; set; }
        public int DealerEmpId { get; set; }
        public int? PerformanceSheetId { get; set; }
        public string? CreatedBy { get; set; }
        public string? CreatedDate { get; set; }
        public int MonthId { get; set; }
        public string FYear { get; set; }
        public List<ActionPlanList> ActionPlanLists { get; set; }
    }
    public class ActionPlanList
    {
        public int Id { get; set; }
        public int? ActionPlanId { get; set; }
        public int? PerformanceSheetId { get; set; }
        public int DealerEmpId { get; set; }
        public int TypeId { get; set; }
        public string? TypeName { get; set; }
        public string? SuperTypeName { get; set; }
        public bool? IsRemarksOnly { get; set; }
        public string? ActionPlan { get; set; }
        public string? Date { get; set; }
        public string? Responsibility { get; set; }
        public string? Remarks { get; set; }
        public bool? IsStart { get; set; }
        public int? RowCount { get; set; }
    }
    public class ActionPlanBasicResult
    {
        public int Id { get; set; }
        public int DealerEmpId { get; set; }
        public int MonthId { get; set; }
        public string FYear { get; set; }
        public bool ActionPlanExists { get; set; }
    }

    public class ActionPlanResult
    {
        public int ActionPlanId { get; set; }
    }
}
