using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Data.Models.RequestModels
{
    public class RequestSubmissionResult
    {
        public int RequestId { get; set; }
        public string RequestNo { get; set; }
        public string RequestTypeName { get; set; }
        public string DealerName { get; set; }
        public string DealerLocation { get; set; }
        public string DealerState { get; set; }
        public string EmpNo { get; set; }
        public List<EmailModel> HOEmails { get; set; }
        public List<EmailModel> CCEmails { get; set; }
        public EmailModel DealerEmail { get; set; }
    }
}
