using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class ActualClaimModel
    {
        public int ActivityId { get; set; }
        public string RequestNo { get; set; }
        public string EmpNo { get; set; }
        public int ClaimId { get; set; }
        public string ClaimNo { get; set; }
        public string ActivityNo { get; set; }
        public string ClaimBy { get; set; }
        public string DealerNo { get; set; }
        public string DealerLocation { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedDateString { get; set; }
        public string DateOfApproval { get; set; }
        public string ActivityType { get; set; }
        public string ActivityThrough { get; set; }
        public string BudgetRequested { get; set; }
        public string ActivityMonth { get; set; }
        public string ActualExpenses { get; set; }
        public string DateOfActivity { get; set; }
        public string CustomerContacted { get; set; }
        public string Enquiry { get; set; }
        public string Delivery { get; set; }
        public string ActualClaimBy { get; set; }
        public DateTime ActualClaimOn { get; set; }
        public int StatusId { get; set; } = 0;
        public string Status { get; set; }
        public bool IsRejected { get; set; }
        public string RejectedBy { get; set; }
        public string RejectedRemarks { get; set; }
        public string RejectedOn { get; set; }
        public string Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public string Image1Ext { get; set; }
        public string Image2Ext { get; set; }
        public string Image3Ext { get; set; }
        public string Remarks { get; set; }

    }
    public class ActualClaimImages
    {
        public string ImageName { get; set; }
    }
}
