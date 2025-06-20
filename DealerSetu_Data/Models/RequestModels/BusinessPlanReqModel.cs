using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class BusinessPlanReqModel
    {
        public int DealerEmpId { get; set; }
        public required string FYear { get; set; }
    }
}
