using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class RequestModel
    {
        public int RequestId { get; set; }
        public int RequestTypeId { get; set; }
        public int HpCategoryId { get; set; }
        public string RequestType { get; set; }
        public string HpCategory { get; set; }
        public string Message { get; set; }
        public bool IsSatisfy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string Name { get; set; }
        public string RequestNo { get; set; }
        //public int Value { get; set; }
        public DateTime? ResponseOn { get; set; }
        public string ResponseDate { get; set; }
        public string State { get; set; }
        public string Status { get; set; }
        //public string Days { get; set; }

    }
}
