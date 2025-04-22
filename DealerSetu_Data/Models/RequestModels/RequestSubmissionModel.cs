using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class RequestSubmissionModel
    {
        public string RequestTypeId { get; set; }
        public string Message { get; set; }
        public string? HpCategory { get; set; }
    }
}
