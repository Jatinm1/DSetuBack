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
        public IFormFile? FileName { get; set; }
        public string? PolicyName { get; set; }
    }
}
