using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DealerSetu_Data.Middleware
{
    public class JWTInactivityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JWTInactivityMiddleware> _logger;
        private readonly string _connString;
        private readonly string _secretKey;
        private readonly int _inactivityTimeoutSeconds;

        // Paths to skip token validation (e.g., Login API)
        private readonly HashSet<string> _excludedTokenValidationPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Login/LoginUser",
            "/api/Login/LoginUser",
            "/swagger",
            "/swagger/index.html",
            "/swagger/v1/swagger.json",
            "/health",
            "/Login/init-csrf",
        };

        // Paths to skip inactivity checks (e.g., Heartbeat API)
        private readonly HashSet<string> _excludedInactivityCheckPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Login/LoginHeartBeat",
            "/api/Login/LoginHeartBeat",
            "/swagger",
            "/health",
            "/Login/init-csrf",
        };

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

        public JWTInactivityMiddleware(
            RequestDelegate next,
            IConfiguration configuration,
            ILogger<JWTInactivityMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
            _connString = _configuration.GetConnectionString("dbDealerSetuEntities");
            _secretKey = _configuration["Jwt:Key"];
            _inactivityTimeoutSeconds = _configuration.GetValue<int>("InactivitySettings:TimeoutSeconds");

            if (string.IsNullOrEmpty(_connString))
            {
                throw new InvalidOperationException("Database connection string is not set. Check appsettings.json.");
            }

            if (string.IsNullOrEmpty(_secretKey))
            {
                throw new InvalidOperationException("JWT secret key is not set. Check appsettings.json.");
            }
        }

        private bool ShouldSkipAntiforgeryValidation(string path)
        {
            return _excludedAntiforgeryPaths.Any(excludedPath =>
                path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value;

            // Skip token validation for excluded paths (e.g., Login API)
            if (ShouldSkipTokenValidation(path))
            {
                await _next(context);
                return;
            }

            try
            {
                // Read JWT token from HTTP-only cookie instead of Authorization header
                var token = context.Request.Cookies["jwt"];

                // Fallback to Authorization header if cookie is not present (for backward compatibility)
                if (string.IsNullOrEmpty(token))
                {
                    token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                }

                if (string.IsNullOrEmpty(token))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "No token provided" });
                    return;
                }

                // Validate token for all other requests
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
                    ClockSkew = TimeSpan.Zero
                };

                var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtToken)
                {
                    var empNo = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.NameId)?.Value;

                    if (string.IsNullOrEmpty(empNo))
                    {
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "Invalid token claims" });
                        return;
                    }

                    // Skip inactivity check for excluded paths (e.g., Heartbeat API)
                    if (!ShouldSkipInactivityCheck(path))
                    {
                        // Check for inactivity
                        if (await IsUserInactive(empNo))
                        {
                            await LogoutUser(empNo);

                            // Clear all authentication cookies with proper options
                            context.Response.Cookies.Delete("jwt", new CookieOptions
                            {
                                HttpOnly = true,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Path = "/"
                            });
                            context.Response.Cookies.Delete("isAuthenticated", new CookieOptions
                            {
                                HttpOnly = false,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Path = "/"
                            });
                            context.Response.Cookies.Delete("empNo", new CookieOptions
                            {
                                HttpOnly = false,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Path = "/"
                            });
                            context.Response.Cookies.Delete("userName", new CookieOptions
                            {
                                HttpOnly = false,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Path = "/"
                            });
                            context.Response.Cookies.Delete("userRole", new CookieOptions
                            {
                                HttpOnly = false,
                                Secure = true,
                                SameSite = SameSiteMode.None,
                                Path = "/"
                            });

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            await context.Response.WriteAsJsonAsync(new { message = "Session expired due to inactivity" });
                            return;
                        }
                    }

                    // Add claims to HttpContext for downstream use
                    context.Items["EmpNo"] = empNo;
                    context.Items["RoleId"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "RoleId")?.Value;
                    context.Items["UserId"] = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

                    await _next(context);
                }
            }
            catch (SecurityTokenExpiredException)
            {
                _logger.LogWarning("Token has expired.");

                // Clear cookies with proper options on token expiration
                context.Response.Cookies.Delete("jwt", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });
                context.Response.Cookies.Delete("isAuthenticated", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });
                context.Response.Cookies.Delete("empNo", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });
                context.Response.Cookies.Delete("userName", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });
                context.Response.Cookies.Delete("userRole", new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Path = "/"
                });

                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Token has expired" });
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token or checking inactivity.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { message = "Invalid token or inactivity check failed" });
                return;
            }
        }

        private bool ShouldSkipTokenValidation(string path)
        {
            return _excludedTokenValidationPaths.Contains(path);
        }

        private bool ShouldSkipInactivityCheck(string path)
        {
            return _excludedInactivityCheckPaths.Contains(path);
        }

        private async Task<bool> IsUserInactive(string empNo)
        {
            using var connection = new SqlConnection(_connString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@EmpNo", empNo);
            parameters.Add("@TimeoutSeconds", _inactivityTimeoutSeconds);
            parameters.Add("@IsInactive", dbType: System.Data.DbType.Boolean, direction: System.Data.ParameterDirection.Output);

            await connection.ExecuteAsync(
                "sp_SESSION_CheckUserInactivity",
                parameters,
                commandType: System.Data.CommandType.StoredProcedure
            );

            return parameters.Get<bool>("@IsInactive");
        }

        private async Task LogoutUser(string empNo)
        {
            // Update the database
            using var connection = new SqlConnection(_connString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@EmpNo", empNo);

            await connection.ExecuteAsync(
                "sp_LOGIN_LogoutUser",
                parameters,
                commandType: System.Data.CommandType.StoredProcedure
            );
        }
    }
}