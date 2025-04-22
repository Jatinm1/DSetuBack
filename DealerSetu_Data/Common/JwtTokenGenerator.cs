using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;


using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using DealerSetu_Data.Models.HelperModels;

namespace DealerSetu_Data.Common
{
    public class JwtTokenGenerator
    {
        public string GenerateJsonWebToken(TokenHelperModel user, IConfiguration configuration)
        {
            try
            {
                var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]));

                var claims = new[]
                {
                 new Claim(JwtRegisteredClaimNames.NameId, user.EmpNo.ToString()),  // Use NameId or custom UserId
                 new Claim(JwtRegisteredClaimNames.Sub, user.EmpNo),
                 new Claim(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                 new Claim("UserId", user.UserId.ToString(), ClaimValueTypes.Integer),
                 new Claim("RoleId", user.RoleId.ToString()),  // Optional custom claim
                 new Claim("Role", user.Role.ToString()),  // Optional custom claim

                 new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),  // Unique token ID
                };

                var jwt = new JwtSecurityToken(
                    issuer: configuration["Jwt:Issuer"],
                    audience: configuration["Jwt:Audience"],
                    claims: claims,
                    notBefore: DateTime.UtcNow,
                    expires: DateTime.UtcNow.AddMinutes(30),  // Token expiration time
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

                return new JwtSecurityTokenHandler().WriteToken(jwt);
            }
            catch (Exception ex)
            {
                throw;  // Use throw; to retain original exception stack trace
            }
        }
    }
}
