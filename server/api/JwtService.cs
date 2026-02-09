using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace api;

public class JwtService(IConfiguration configuration)
{
    public string GenerateToken(string userId, string role)
    {
        var secret = configuration["AppOptions:Secret"]
                     ?? throw new InvalidOperationException("JWT Secret not configured (AppOptions:Secret)");

        return new JwtSecurityTokenHandler().WriteToken(
            new JwtSecurityToken(
                claims: new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                    new Claim(ClaimTypes.Role, role),
                },
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    SecurityAlgorithms.HmacSha256
                )
            )
        );
    }
}