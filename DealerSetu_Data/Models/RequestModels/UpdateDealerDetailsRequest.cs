using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class UpdateDealerDetailsRequest
    {
        public int? UserId { get; set; }
        public string? EmpName { get; set; }
        public string? Location { get; set; }
        public string? District { get; set; }
        public string? Zone { get; set; }
        public string? State { get; set; }
    }

    public class UpdateEmployeeDetailsRequest
    {
        public int? UserId { get; set; }
        public string? EmpName { get; set; }
        public string? Email { get; set; }

    }

}
