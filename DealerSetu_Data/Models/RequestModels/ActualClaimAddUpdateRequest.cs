using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class ActualClaimAddUpdateRequest
    {
        [Required]
        public int ActivityId { get; set; }
        public string ActualExpenses { get; set; }

        public string? DateOfActivity { get; set; } = DateTime.Now.ToString();

        public string CustomerContacted { get; set; }

        public string Enquiry { get; set; }

        public string Delivery { get; set; }

        // File uploads
        public IFormFile? Image1 { get; set; }

        public IFormFile? Image2 { get; set; }

        public IFormFile? Image3 { get; set; }
    }
}
