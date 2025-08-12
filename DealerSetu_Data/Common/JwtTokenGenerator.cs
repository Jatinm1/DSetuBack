using DealerSetu_Data.Models;
using DealerSetu_Data.Models.HelperModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));
                var claims = new[]
                {
            new Claim(JwtRegisteredClaimNames.NameId, user.EmpNo.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.EmpNo),
            new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("UserId", user.UserId.ToString(), ClaimValueTypes.Integer),
            new Claim("RoleId", user.RoleId.ToString()),
            new Claim("Role", user.Role.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

                var jwt = new JwtSecurityToken(
                    issuer: configuration["Jwt:Issuer"],
                    audience: configuration["Jwt:Audience"],
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(30),
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
                );

                var token = new JwtSecurityTokenHandler().WriteToken(jwt);

                // Use consistent cookie options for setting JWT
                var jwtCookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    Path = "/",
                    Domain = null // Make sure this is consistent
                };

                _httpContextAccessor.HttpContext?.Response.Cookies.Append("jwt", token, jwtCookieOptions);

                // Set other cookies with consistent options
                var regularCookieOptions = new CookieOptions
                {
                    HttpOnly = false,
                    Secure = true,
                    SameSite = SameSiteMode.None,
                    Expires = DateTime.UtcNow.AddMinutes(30),
                    Path = "/"
                };

                _httpContextAccessor.HttpContext?.Response.Cookies.Append("isAuthenticated", "true", regularCookieOptions);
                _httpContextAccessor.HttpContext?.Response.Cookies.Append("empNo", user.EmpNo, regularCookieOptions);
                _httpContextAccessor.HttpContext?.Response.Cookies.Append("userName", user.UserName, regularCookieOptions);
                _httpContextAccessor.HttpContext?.Response.Cookies.Append("userRole", user.Role, regularCookieOptions);

                return token;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}