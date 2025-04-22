using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DealerSetu_Data.Models.ViewModels;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ClaimUpdationRequest
    {
        public int claimId { get; set; }
        public List<ActivityModel> ActivityData { get; set; }
    }
}
