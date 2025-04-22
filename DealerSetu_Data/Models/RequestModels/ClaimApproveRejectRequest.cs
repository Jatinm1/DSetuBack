using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ClaimApproveRejectRequest
    {
        public int ClaimId { get; set; }
        public bool IsApproved { get; set; }
        public string? RejectRemarks { get; set; }
    }
}
