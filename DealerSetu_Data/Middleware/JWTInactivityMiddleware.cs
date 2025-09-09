using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Middleware
{
    public class JWTSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JWTSecurityMiddleware> _logger;
        private readonly string _connectionString;
        private readonly string _secretKey;

        // Paths to skip token validation (e.g., Login API, Public endpoints)
        private readonly HashSet<string> _excludedTokenValidationPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Login/LoginUser",
            "/api/Login/LoginUser",
            "/swagger",
            "/swagger/index.html",
            "/swagger/v1/swagger.json",
            "/health",
            "/Login/init-csrf",
            "/api/auth/login",
            "/api/public"
        };

        // Paths to skip antiforgery validation
        private readonly HashSet<string> _excludedAntiforgeryPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Login/LoginUser",
            "/api/Login/LoginUser",
            "/Login/LoginHeartBeat",
            "/api/Login/LoginHeartBeat",
            "/swagger",
            "/health",
            "/Login/init-csrf",
        };

        // Paths to skip session security checks (IP/UserAgent/Tab validation)
        private readonly HashSet<string> _excludedSessionCheckPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/swagger",
            "/health",
            "/Login/init-csrf"
        };

        public JWTSecurityMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<JWTSecurityMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;

            // Get connection string with fallback (maintaining backward compatibility)
            _connectionString = _configuration.GetConnectionString("dbDealerSetuEntities");
            _secretKey = _configuration["Jwt:Key"];

            // Validate required configuration
            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured. Check appsettings.json.");
            }

            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException("JWT secret key is not configured. Check appsettings.json.");
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Log origin if present for debugging
            if (context.Request.Headers.ContainsKey("Origin"))
            {
                var origin = context.Request.Headers["Origin"].ToString();
                _logger.LogDebug("Request origin: {Origin}", origin);
            }

            // Skip token validation for excluded paths (e.g., Login API)
            if (ShouldSkipTokenValidation(path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Read JWT token from Authorization header first (primary method for normal JWT)
                var token = ExtractTokenFromHeader(context);

                // Fallback to regular cookies if Authorization header is not present
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Cookies["jwt"];
                }

                if (string.IsNullOrEmpty(token))
                {
                    await WriteUnauthorizedResponse(context, "No token provided");
                    return;
                }

                // Validate token format and claims
                var jwtToken = await ValidateJwtToken(token);
                if (jwtToken == null)
                {
                    await WriteUnauthorizedResponse(context, "Invalid or expired token");
                    return;
                }

                // Extract user information from token
                var empNo = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value;
                if (string.IsNullOrEmpty(empNo))
                {
                    await WriteUnauthorizedResponse(context, "Invalid token claims");
                    return;
                }

                // Check session security (IP, UserAgent, TabId) if not excluded
                if (!ShouldSkipSessionCheck(path))
                {
                    var sessionValidation = await ValidateSessionSecurity(context, jwtToken, empNo);
                    if (!sessionValidation.IsValid)
                    {
                        await WriteUnauthorizedResponse(context, sessionValidation.ErrorMessage);
                        return;
                    }
                }

                // Add claims to HttpContext for downstream use (enhanced version)
                AddClaimsToContext(context, jwtToken);

                await _next(context);
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token has expired for path: {Path}", path);
                await WriteUnauthorizedResponse(context, "Token has expired");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing JWT middleware for path: {Path}", path);
                await WriteServerErrorResponse(context, "Authentication error occurred");
            }
        }

        private string? ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }

        private async Task<JwtSecurityToken?> ValidateJwtToken(string token)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = _configuration["Jwt:Audience"],
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey)),
                    ClockSkew = TimeSpan.Zero // Remove default 5-minute clock skew
                };

                var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);
                return validatedToken as JwtSecurityToken;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "JWT token validation failed");
                return null;
            }
        }

        private async Task<(bool IsValid, string ErrorMessage)> ValidateSessionSecurity(HttpContext context, JwtSecurityToken jwtToken, string empNo)
        {
            try
            {
                // Get stored values from token claims
                var storedIpAddress = jwtToken.Claims.FirstOrDefault(c => c.Type == "IPAddress")?.Value;
                var storedUserAgent = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserAgent")?.Value;
                var storedTabId = jwtToken.Claims.FirstOrDefault(c => c.Type == "TabId")?.Value;

                // Get current values from request
                var currentUserAgent = context.Request.Headers["User-Agent"].ToString();
                var currentIpAddress = context.Connection.RemoteIpAddress?.ToString();
                var currentTabId = context.Request.Headers["X-Tab-Id"].ToString();

                // Check Tab ID first (most restrictive)
                if (!string.IsNullOrEmpty(storedTabId) && !string.IsNullOrEmpty(currentTabId))
                {
                    if (currentTabId != storedTabId)
                    {
                        _logger.LogWarning("Tab ID mismatch for user {EmpNo}. Stored: {StoredTabId}, Current: {CurrentTabId}. Logging out user.",
                            empNo, storedTabId, currentTabId);

                        // Logout user immediately due to tab mismatch
                        await LogoutUser(empNo);
                        return (false, "Authentication failed. Please login again.");
                    }
                }

                // Check IP and User Agent combination
                if (!string.IsNullOrEmpty(storedIpAddress) && !string.IsNullOrEmpty(storedUserAgent))
                {
                    if (storedIpAddress != currentIpAddress && storedUserAgent != currentUserAgent)
                    {
                        _logger.LogWarning("IP and UserAgent mismatch for user {EmpNo}. IP - Stored: {StoredIp}, Current: {CurrentIp}. UA - Stored: {StoredUA}, Current: {CurrentUA}. Logging out user.",
                            empNo, storedIpAddress, currentIpAddress,
                            storedUserAgent?.Substring(0, Math.Min(50, storedUserAgent.Length)),
                            currentUserAgent?.Substring(0, Math.Min(50, currentUserAgent.Length)));

                        // Logout user immediately due to IP/UserAgent mismatch
                        await LogoutUser(empNo);
                        return (false, "Authentication failed. Please login again.");
                    }
                }

                return (true, "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating session security for user {EmpNo}", empNo);
                return (false, "Authentication failed. Please login again.");
            }
        }

        private bool ShouldSkipAntiforgeryValidation(string path)
        {
            return _excludedAntiforgeryPaths.Any(excludedPath =>
                path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldSkipTokenValidation(string path)
        {
            return _excludedTokenValidationPaths.Any(excludedPath =>
                path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        private bool ShouldSkipSessionCheck(string path)
        {
            return _excludedSessionCheckPaths.Any(excludedPath =>
                path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        private async Task LogoutUser(string empNo)
        {
            try
            {
                if (string.IsNullOrEmpty(empNo))
                {
                    _logger.LogWarning("Cannot logout user - EmpNo is null or empty");
                    return;
                }

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@EmpNo", empNo);

                await connection.ExecuteAsync(
                    "sp_LOGIN_LogoutUser",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                _logger.LogInformation("User {EmpNo} logged out due to security validation failure", empNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out user {EmpNo}", empNo);
            }
        }

        private void AddClaimsToContext(HttpContext context, JwtSecurityToken jwtToken)
        {
            // Add common claims to HttpContext for downstream use (enhanced version)
            context.Items["EmpNo"] = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value;
            context.Items["RoleId"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;
            context.Items["UserId"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            context.Items["IPAddress"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "IPAddress")?.Value;
            context.Items["UserAgent"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserAgent")?.Value;
            context.Items["TabId"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "TabId")?.Value;

            // Also add to User.Identity for compatibility with authorization attributes
            var claims = jwtToken.Claims.Select(c => new Claim(c.Type, c.Value));
            var identity = new ClaimsIdentity(claims, "jwt");
            context.User = new ClaimsPrincipal(identity);
        }

        private async Task WriteUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new { ErrorMessage = message, Status = 401 };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private async Task WriteServerErrorResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            var response = new { ErrorMessage = message, Status = 500 };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}