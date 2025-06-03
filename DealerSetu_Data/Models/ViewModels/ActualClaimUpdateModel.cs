using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.ViewModels
{
    public class ActualClaimUpdateModel
    {
        public int ClaimId { get; set; }
        public string? EmpNo { get; set; }
        public string? ActualExpenses { get; set; }
        public string? DateOfActivity { get; set; }
        public string? CustomerContacted { get; set; }
        public string? Enquiry { get; set; }
        public string? Delivery { get; set; }
        public string? Image1 { get; set; }
        public string? Image2 { get; set; }
        public string? Image3 { get; set; }
        public DateTime ActualClaimOn { get; set; }
    }
}
