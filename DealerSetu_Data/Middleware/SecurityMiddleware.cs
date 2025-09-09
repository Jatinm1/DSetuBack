using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

public class SecurityMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityMiddleware> _logger;
    private readonly string[] _allowedHosts;
    private readonly ConcurrentDictionary<string, List<DateTime>> _requestLog = new();
    private readonly int _maxRequestsPerMinute = 50;
    private readonly int _maxInputLength = 10000;

    public SecurityMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<SecurityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _allowedHosts = configuration.GetSection("AllowedHosts").Get<string[]>() ?? Array.Empty<string>();

        if (_allowedHosts.Length == 0)
        {
            throw new ArgumentException("AllowedHosts configuration is required and cannot be empty.");
        }
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            var clientIp = GetClientIpAddress(context);

            // 1. Dynamic Rate Limiting (application-level)
            if (IsRateLimited(clientIp))
            {
                //LogSecurityEvent("RateLimit", $"Rate limit exceeded for IP: {clientIp}", context);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await context.Response.WriteAsync("Too Many Requests");
                return;
            }

            // 2. Host Header Validation (business logic)
            if (!IsValidHost(context))
            {
                //LogSecurityEvent("InvalidHost", $"Invalid host header: {context.Request.Host.Value}", context);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Forbidden");
                return;
            }

            // 3. Advanced XSS Protection (content analysis)
            if (IsXssAttempt(context))
            {
                //LogSecurityEvent("XSSAttempt", "Potential XSS attack detected", context);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Bad Request");
                return;
            }

            // 4. Form Input Validation (business logic)
            if (!ValidateFormInputs(context))
            {
                //LogSecurityEvent("InvalidInput", "Form input validation failed", context);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Bad Request");
                return;
            }

            // 5. Add dynamic security headers
            AddDynamicSecurityHeaders(context);

            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Security middleware error");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsync("Internal Server Error");
        }
    }

    private string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool IsRateLimited(string clientIp)
    {
        var now = DateTime.UtcNow;
        var requests = _requestLog.GetOrAdd(clientIp, _ => new List<DateTime>());

        lock (requests)
        {
            requests.RemoveAll(r => (now - r).TotalMinutes > 1);

            if (requests.Count >= _maxRequestsPerMinute)
            {
                return true;
            }

            requests.Add(now);
        }

        return false;
    }

    private bool IsValidHost(HttpContext context)
    {
        var requestHost = context.Request.Host.Value.ToLower();
        return _allowedHosts.Contains(requestHost);
    }

    private bool ValidateFormInputs(HttpContext context)
    {
        // Only validate form inputs, not query strings (handled by web.config)
        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = context.Request.Form;
                foreach (var key in form.Keys)
                {
                    // Key length validation
                    if (key.Length > 100) return false;

                    var values = form[key];
                    foreach (var value in values)
                    {
                        // Value length validation
                        if (value.Length > _maxInputLength) return false;

                        // Business-specific validation
                        if (ContainsBusinessSpecificThreats(value)) return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private bool IsXssAttempt(HttpContext context)
    {
        // Skip file uploads
        if (context.Request.HasFormContentType && context.Request.Form.Files.Any())
        {
            return false;
        }

        // Check query string for XSS (more detailed than web.config)
        if (context.Request.QueryString.HasValue)
        {
            var query = context.Request.QueryString.Value;
            if (ContainsAdvancedXssPatterns(query)) return true;
        }

        // Check path for XSS
        var path = context.Request.Path.Value;
        if (path != null && ContainsAdvancedXssPatterns(path)) return true;

        // Check form values for XSS
        if (context.Request.HasFormContentType)
        {
            try
            {
                var form = context.Request.Form;
                foreach (var key in form.Keys)
                {
                    if (ContainsAdvancedXssPatterns(key)) return true;

                    var values = form[key];
                    foreach (var value in values)
                    {
                        if (ContainsAdvancedXssPatterns(value)) return true;
                    }
                }
            }
            catch
            {
                return true;
            }
        }

        return false;
    }

    private bool ContainsAdvancedXssPatterns(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        string decoded;
        try
        {
            decoded = HttpUtility.UrlDecode(content).ToLower();
        }
        catch
        {
            return true;
        }

        // Advanced XSS patterns (more sophisticated than basic filtering)
        string[] advancedXssPatterns = new[] {
            "<script", "</script>", "javascript:", "data:text/html", "data:application/javascript", "vbscript:",
            "onerror=", "onload=", "onmouseover=", "onfocus=", "onblur=", "onclick=", "onmouseout=", "onkeydown=",
            "onkeyup=", "onkeypress=", "onchange=", "onsubmit=", "eval(", "settimeout(", "setinterval(", "function(",
            "alert(", "confirm(", "prompt(", "document.cookie", "document.write", "document.writeln", "innerhtml",
            "outerhtml", "document.location", "window.location", "location.href", "location.replace", "location.assign",
            "fromcharcode", "string.fromcharcode", "expression(", "behavior:", "binding:", "import",
            "phNjcmlwdd4=", "c2NyaXB0", "amf2yxnjcmlwda==", // Base64 encoded script tags
            "&#x", "&#", "&lt;script", "&gt;", // HTML entity encoding
            "\\u0073\\u0063\\u0072\\u0069\\u0070\\u0074", // Unicode encoding
            "src=data:", "href=data:", "action=data:", // Data URI schemes
            "style=", "background:", "background-image:", // CSS injection
            "expression\\(", "url\\(", "import\\(", // CSS expression attacks
        };

        return advancedXssPatterns.Any(pattern => decoded.Contains(pattern));
    }

    private bool ContainsBusinessSpecificThreats(string content)
    {
        if (string.IsNullOrEmpty(content)) return false;

        string decoded;
        try
        {
            decoded = HttpUtility.UrlDecode(content).ToLower();
        }
        catch
        {
            return true;
        }

        // Business-specific security patterns
        string[] businessThreats = new[] {
            "union select", "drop table", "insert into", "delete from", "update set", // SQL injection
            "<?php", "<?=", "system(", "exec(", "passthru(", "shell_exec(", // Code injection
            "/etc/passwd", "\\windows\\", "c:\\", "cmd.exe", "powershell", "/bin/bash", "/bin/sh", // Path traversal
            "whoami", "net user", "objectclass=", "objectcategory=", // System commands
            "<!entity", "<!doctype", "<!--#", // XXE attacks
        };

        return businessThreats.Any(threat => decoded.Contains(threat));
    }

    private void AddDynamicSecurityHeaders(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Only add headers that need to be dynamic or calculated
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "SAMEORIGIN";
        headers["X-XSS-Protection"] = "1; mode=block";
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=(), usb=(), vr=(), accelerometer=(), gyroscope=(), magnetometer=()";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";

        // Dynamic cache control based on content type or path
        if (context.Request.Path.Value?.Contains("/api/") == true)
        {
            headers["Cache-Control"] = "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0";
            headers["Pragma"] = "no-cache";
            headers["Expires"] = "0";
        }

        // Dynamic CSP based on request context
        var csp = BuildDynamicContentSecurityPolicy(context);
        headers["Content-Security-Policy"] = csp;

        // Add request-specific headers
        headers["X-Request-ID"] = Guid.NewGuid().ToString();
        headers["X-DNS-Prefetch-Control"] = "off";
        headers["X-Download-Options"] = "noopen";
    }

    private string BuildDynamicContentSecurityPolicy(HttpContext context)
    {
        // Build CSP based on request context
        var csp = "default-src 'self'; " +
                 "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                 "style-src 'self' 'unsafe-inline'; " +
                 "img-src 'self' data: https: blob:; " +
                 "connect-src 'self'; " +
                 "font-src 'self'; " +
                 "object-src 'none'; " +
                 "media-src 'self'; " +
                 "frame-src 'none'; " +
                 "form-action 'self'; " +
                 "base-uri 'self'; " +
                 "frame-ancestors 'self';";

        // Add dynamic CSP rules based on request path or user context
        if (context.Request.Path.Value?.Contains("/admin/") == true)
        {
            csp += " upgrade-insecure-requests;";
        }

        return csp;
    }

    //private void LogSecurityEvent(string eventType, string details, HttpContext context)
    //{
    //    var clientIp = GetClientIpAddress(context);
    //    var userAgent = context.Request.Headers["User-Agent"].ToString();
    //    var requestPath = context.Request.Path;
    //    var method = context.Request.Method;
    //    var referer = context.Request.Headers["Referer"].ToString();

    //    _logger.LogWarning("Security Event: {EventType} - {Details} - IP: {IpAddress} - Path: {RequestPath} - Method: {Method} - UserAgent: {UserAgent} - Referer: {Referer}",
    //        eventType, details, clientIp, requestPath, method, userAgent, referer);
    //}
}