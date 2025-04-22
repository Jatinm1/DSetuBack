using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class Rejectrequest
    {
        public string IsRejected { get; set; }
        public string CreatedOn { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<DemoTractorReject> lstreq { get; set; }


    }

    public class DemoTractorReject
    {
        public string IsRejected { get; set; }
        public string CreatedOn { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
