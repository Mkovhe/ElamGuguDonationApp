using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ElamIgugu.Web.Models;
using ElamIgugu.Web.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ElamIgugu.Web.Services;

/// <summary>Creates the signed login token the Android app sends with every request.</summary>
public class TokenService : ITokenService
{
    private readonly JwtOptions _options;

    public TokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiresAtUtc) CreateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.FullName)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
