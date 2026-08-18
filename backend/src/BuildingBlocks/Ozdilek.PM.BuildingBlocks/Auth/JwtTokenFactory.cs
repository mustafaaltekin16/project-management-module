using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Ozdilek.PM.BuildingBlocks.Auth;

/// <summary>
/// Mints a JWT signed with the shared symmetric key (see <see cref="AuthOptions"/>). Used both by the
/// dev-only token issuer and by the real login endpoint (UserDirectoryService) — one place that knows
/// how a valid token for this module looks, so both stay compatible with <see cref="CwaAuthExtensions"/>'s
/// validation rules (audience, "sub"/"role" claim names).
/// </summary>
public static class JwtTokenFactory
{
    public static string CreateToken(AuthOptions options, string userId, string displayName, IEnumerable<string> roles, TimeSpan lifetime)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.DevSigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new("name", displayName)
        };
        claims.AddRange(roles.Select(role => new Claim("role", role)));

        var token = new JwtSecurityToken(
            audience: options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
