using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DealerListRequest
    {
        public string? Keyword { get; set; }
        public int PageIndex { get; set; }
        public int PageSize { get; set; }
    }

}
