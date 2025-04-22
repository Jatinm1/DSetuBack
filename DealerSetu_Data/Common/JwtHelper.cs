using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Common
{
    public class JwtHelper
    {
        public string GetClaimValue(HttpContext context, string claimType)
        {
            return context.Items[claimType]?.ToString();
        }
    }

}
