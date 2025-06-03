using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class ClaimModel
    {
        public int ClaimId { get; set; }
        public string ClaimNo { get; set; }
        public string DealerNo { get; set; }
        public string DealerState { get; set; }
        public string DealerLocation { get; set; }
        public int Value { get; set; }
        public int NoOfActivities { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateString { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public string ActualClaimStatus { get; set; }
        public bool IsRejected { get; set; }
        public string RejectedBy { get; set; }
        public string RejectedRemarks { get; set; }
        public string RejectedOn { get; set; }
        public List<ActivityModel> ActivityDetails { get; set; }
        public string RequestNo { get; set; }
        public string Message { get; set; }
        public int ActualStatusId { get; set; }
        public string Remarks { get; set; }
    }

    public class ActivityModel
    {
        public int ActivityId { get; set; }
        public int ClaimId { get; set; }
        public string? ActivityType { get; set; }
        public string? ActivityThrough { get; set; }
        public string? BudgetRequested { get; set; }
        public string? ActivityMonth { get; set; }
        public DateTime CreatedDate { get; set; }
        public int StatusId { get; set; }
    }

    // For creating new claims (strict validation)
    public class CreateActivityModel
    {
        public int ActivityId { get; set; }
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "ActivityType is required")]
        public string ActivityType { get; set; }

        [Required(ErrorMessage = "ActivityThrough is required")]
        public string ActivityThrough { get; set; }

        [Required(ErrorMessage = "BudgetRequested is required")]
        public string BudgetRequested { get; set; }

        [Required(ErrorMessage = "ActivityMonth is required")]
        public string ActivityMonth { get; set; }

        public DateTime CreatedDate { get; set; }
        public int StatusId { get; set; }
    }

    // For updating claims (all optional)
    public class UpdateActivityModel
    {
        public int ActivityId { get; set; }
        public int ClaimId { get; set; }
        public string? ActivityType { get; set; }
        public string? ActivityThrough { get; set; }
        public string? BudgetRequested { get; set; }
        public string? ActivityMonth { get; set; }
        public DateTime CreatedDate { get; set; }
        public int StatusId { get; set; }
    }
}
