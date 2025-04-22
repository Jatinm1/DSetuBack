using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DemoReqSubmissionModel
    {
        public string RequestNo { get; set; }
        public string DealerNo { get; set; }
        public string ModelRequested { get; set; }
        public string Reason { get; set; }
        public string SchemeType { get; set; }
        public string SpecialVariant { get; set; }
        public int ImplementRequired { get; set; }
        public int ImplementId { get; set; }
        public string Message { get; set; }
        public int HpCategoryId { get; set; }
    }
}
