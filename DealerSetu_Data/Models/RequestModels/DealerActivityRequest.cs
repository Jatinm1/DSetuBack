using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DealerActivityRequest
    {
        public int? DropdownSelection { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string ReqNo { get; set; }
        public bool Export { get; set; }
        public int? PageIndex { get; set; }
        public int? PageSize { get; set; }
    }

}
