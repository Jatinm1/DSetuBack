using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ReportRequest
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

    public class RejectListRequest
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }


}
