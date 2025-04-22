using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DealerSetu_Data.Common.RegularExpressions;

namespace DealerSetu_Data.Models.HelperModels
{
    public class PolicyUploadModel
    {
        public IFormFile? RecomendationFileName { get; set; }
        [AlphaNumeric]
        public string? UpdatedName { get; set; }
    }
}
