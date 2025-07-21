using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Middleware
{
    public class AntiforgeryMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IAntiforgery _antiforgery;
        private readonly ILogger<AntiforgeryMiddleware> _logger;
        private readonly List<string> _excludedPaths = new List<string>
    {
        "/Login/init-csrf",
        "/Login/LoginUser"
    };

        public AntiforgeryMiddleware(RequestDelegate next, IAntiforgery antiforgery, ILogger<AntiforgeryMiddleware> logger)
        {
            _next = next;
            _antiforgery = antiforgery;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (ShouldValidateToken(context))
            {
                try
                {
                    await _antiforgery.ValidateRequestAsync(context);
                }
                catch (AntiforgeryValidationException ex)
                {
                    _logger.LogWarning(ex, "XSRF token validation failed");
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("XSRF token validation failed");
                    return;
                }
            }

            await _next(context);
        }

        private bool ShouldValidateToken(HttpContext context)
        {
            // Skip validation for excluded paths and non-POST/PUT/PATCH/DELETE methods
            return !_excludedPaths.Contains(context.Request.Path.Value) &&
                   (context.Request.Method == HttpMethods.Post ||
                    context.Request.Method == HttpMethods.Put ||
                    context.Request.Method == HttpMethods.Patch ||
                    context.Request.Method == HttpMethods.Delete);
        }
    }
}
