using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;

namespace DealerSetu_Data.Middleware
{
    public class ConnectionValidation
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _config;
        private readonly ILogger<ConnectionValidation> _logger;
        private readonly string _connectionString;
        private readonly int _inactivityTimeoutSeconds;

        // Paths to skip inactivity checks (e.g., Heartbeat API, Login endpoints)
        private readonly HashSet<string> _excludedInactivityCheckPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "/Login/LoginUser",
            "/api/Login/LoginUser",
            "/Login/LoginHeartBeat",
            "/api/Login/LoginHeartBeat",
            "/HeartBeat",
            "/api/heartbeat",
            "/swagger",
            "/health",
            "/Login/init-csrf",
            "/api/public"
        };

        

        public ConnectionValidation(RequestDelegate next, IConfiguration configuration, ILogger<ConnectionValidation> logger)
        {
            _next = next;
            _config = configuration;
            _logger = logger;

            _connectionString = _config.GetConnectionString("dbDealerSetuEntities");
            _inactivityTimeoutSeconds = _config.GetValue<int>("InactivitySettings:TimeoutSeconds", 1800); // Default 30 minutes

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new InvalidOperationException("Database connection string is not configured. Check appsettings.json.");
            }
        }

        public async Task Invoke(HttpContext context)
        {
            var path = context.Request.Path.Value ?? "";

            // Validate origin if present
            if (context.Request.Headers.ContainsKey("Origin"))
            {
                var origin = context.Request.Headers["Origin"].ToString();                
            }

            // Skip inactivity checks for excluded paths
            if (ShouldSkipInactivityCheck(path))
            {
                await _next(context);
                return;
            }

            // Check for inactivity only if user is authenticated
            var empNo = GetEmpNoFromContext(context);
            if (!string.IsNullOrEmpty(empNo))
            {
                try
                {
                    // Check for user inactivity
                    if (!await ValidateConnectionAsync(empNo))
                    {
                        await LogoutUser(empNo);
                        await WriteUnauthorizedResponse(context, "Session expired due to inactivity");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking inactivity for user {EmpNo}", empNo);
                    // Continue processing - don't block user for inactivity check errors
                }
            }

            await _next(context);
        }      

        private bool ShouldSkipInactivityCheck(string path)
        {
            return _excludedInactivityCheckPaths.Any(excludedPath =>
                path.StartsWith(excludedPath, StringComparison.OrdinalIgnoreCase));
        }

        private string? GetEmpNoFromContext(HttpContext context)
        {
            // Try to get EmpNo from context items (set by JWT middleware)
            if (context.Items.ContainsKey("EmpNo"))
            {
                return context.Items["EmpNo"]?.ToString();
            }

            // Fallback: Try to extract from token if JWT middleware hasn't run yet
            var token = context.Request.Cookies["jwt"];
            if (string.IsNullOrEmpty(token))
            {
                token = ExtractTokenFromHeader(context);
            }

            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var jwtToken = tokenHandler.ReadToken(token) as JwtSecurityToken;
                    return jwtToken?.Claims.FirstOrDefault(c => c.Type == "nameid" || c.Type == JwtRegisteredClaimNames.NameId)?.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not extract EmpNo from token for inactivity check");
                }
            }

            return null;
        }

        private string? ExtractTokenFromHeader(HttpContext context)
        {
            var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return null;

            return authHeader.Substring("Bearer ".Length).Trim();
        }        

        private async Task<bool> ValidateConnectionAsync(string tokenNo)
        {
            try
            {
                using var db = new SqlConnection(_connectionString);
                await db.OpenAsync();
                var result = await db.QueryFirstOrDefaultAsync<int>(
                    "sp_ValidateConnection",
                    new { EmpNo = tokenNo },
                    commandType: CommandType.StoredProcedure
                );
                return result == 1; // true if active, false if inactive
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating connection for EmpNo: {EmpNo}", tokenNo);
                return false;
            }
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

                _logger.LogInformation("User {EmpNo} logged out due to inactivity", empNo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging out user {EmpNo}", empNo);
            }
        }

        private async Task WriteUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            var response = new { ErrorMessage = message, Status = 401 };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }

        private async Task WriteErrorResponse(HttpContext context, string message)
        {
            context.Response.ContentType = "application/json";
            var response = new { ErrorMessage = message, Status = context.Response.StatusCode };
            await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        }
    }
}