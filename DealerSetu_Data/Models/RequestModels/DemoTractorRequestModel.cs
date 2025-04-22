using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DemoTractorRequestModel
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? RequestNo { get; set; }
        public string? State { get; set; }
        public string? Status { get; set; }
        public string? Fyear { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
        public bool Export { get; set; } = false;

    }
}
