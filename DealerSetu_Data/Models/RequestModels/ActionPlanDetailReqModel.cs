using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ActionPlanDetailReqModel
    {
        public int ActionPlanId { get; set; }
        public int DealerEmpId { get; set; }
        public int Month { get; set; }
        public required string FYear { get; set; }
    }
}
