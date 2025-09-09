using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Data.Common
{
    public class JwtTokenGenerator
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public JwtTokenGenerator(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string GenerateJsonWebToken(TokenHelperModel user, IConfiguration configuration)
        {
            try
            {
                var context = _httpContextAccessor.HttpContext;
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
                string ipAddress = context.Connection.RemoteIpAddress?.ToString();
                string userAgent = context.Request.Headers["User-Agent"].ToString();
                string tabId = context.Request.Headers["X-Tab-Id"].ToString();

                var claims = new[]
                {
                    new Claim(JwtRegisteredClaimNames.NameId, user.EmpNo.ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.EmpNo),
                    new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                    new Claim("UserId", user.UserId.ToString(), ClaimValueTypes.Integer),
                    new Claim("RoleId", user.RoleId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("Role", user.Role.ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim("IPAddress", ipAddress ?? ""),
                    new Claim("UserAgent", userAgent ?? ""),
                    new Claim("TabId", tabId ?? "")
                };

                var jwt = new JwtSecurityToken(
                    issuer: configuration["Jwt:Issuer"],
                    audience: configuration["Jwt:Audience"],
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

                return new JwtSecurityTokenHandler().WriteToken(jwt);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}