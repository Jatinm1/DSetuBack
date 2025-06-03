using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ClaimSubmissionRequest
    {
        public string RequestNo { get; set; }
        public string DealerNo { get; set; }
        public List<ActivityModel> ActivityData { get; set; }
    }

    public class ClaimUpdateRequest
    {
        public string RequestNo { get; set; }
        public string DealerNo { get; set; }
        public List<UpdateActivityModel> ActivityData { get; set; }
    }
}
