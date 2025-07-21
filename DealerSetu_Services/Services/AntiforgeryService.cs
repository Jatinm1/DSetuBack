using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Services.Services
{
    public class AntiforgeryService
    {
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<AntiforgeryService> _logger;

        public AntiforgeryService(IAntiforgery antiforgery, ILogger<AntiforgeryService> logger)
        {
            _antiforgery = antiforgery;
            _logger = logger;
        }

        public void ValidateTokens(HttpContext httpContext)
        {
            try
            {
                _antiforgery.ValidateRequestAsync(httpContext).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "XSRF token validation failed");
                throw new AntiforgeryValidationException("XSRF token validation failed");
            }
        }
    }

    public class AntiforgeryValidationException : Exception
    {
        public AntiforgeryValidationException(string message) : base(message) { }
    }
}
