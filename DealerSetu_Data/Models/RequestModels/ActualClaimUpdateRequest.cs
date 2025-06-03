using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ActualClaimUpdateRequest
    {
        public int ClaimId { get; set; } // Required for update
        public string? ActualExpenses { get; set; }
        public string? DateOfActivity { get; set; }
        public string? CustomerContacted { get; set; }
        public string? Enquiry { get; set; }
        public string? Delivery { get; set; }
        // File uploads
        public IFormFile? Image1 { get; set; }
        public IFormFile? Image2 { get; set; }
        public IFormFile? Image3 { get; set; }
    }
}
