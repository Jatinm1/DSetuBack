using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class PerformanceSheetReqModel
    {
        public int DealerEmpId { get; set; }
        public int Month { get; set; }
        public string FYear { get; set; }
    }
}
