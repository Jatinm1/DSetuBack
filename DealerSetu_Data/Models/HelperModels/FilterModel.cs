using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.HelperModels
{
    public class FilterModel
    {
        public string RequestTypeId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string RoleId { get; set; }
        public string EmpNo { get; set; }
        public string RequestNo { get; set; }
        public string ClaimNo { get; set; }
        public int ClaimId { get; set; }
        public int ActivityId { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        public int ReqId { get; set; }
        public string Fyear { get; set; }
        public bool? Export { get; set; }
        public bool? IsApproved { get; set; }
        public string? RejectRemarks { get; set; }

    }
}
