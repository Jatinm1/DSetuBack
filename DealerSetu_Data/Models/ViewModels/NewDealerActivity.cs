using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class NewDealerActivity
    {
        public string RequestNo { get; set; }

        public DateTime CreatedOn { get; set; }
        public string CreatedDate { get; set; }
        public DateTime TMDate { get; set; }
        public string TMDateResponse { get; set; }

        public DateTime CCMDate { get; set; }
        public string CCMDateResponse { get; set; }

        public DateTime CMDate { get; set; }
        public string CMDateResponse { get; set; }
        public DateTime HODate { get; set; }
        public string HODateResponse { get; set; }
        public int ClaimId { get; set; }

    }
}
