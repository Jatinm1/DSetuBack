using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Models.RequestModels
{
    public class DealersRequestModel
    {
        public string? Keyword { get; set; }

        [Required]
        public int Month { get; set; }

        [Required]
        public string FYear { get; set; } = string.Empty;
        [Required]
        public int PageIndex { get; set; }
        [Required]
        public int PageSize { get; set; }
    }
}
