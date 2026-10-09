using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TicketDesk.Api.Domain;

namespace TicketDesk.Api.Services;

public class TokenService(IConfiguration cfg)
{
    public string Create(User user)
    {
        var jwt = cfg.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

        // short claim names on purpose; Program.cs maps NameClaimType/RoleClaimType to these
        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("name", user.FullName),
            new("role", user.Role)
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(int.TryParse(jwt["ExpiryHours"], out var h) ? h : 12),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
