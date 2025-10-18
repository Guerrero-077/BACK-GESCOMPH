namespace Business.Services.SecurityAuthentication.Tokens;

using Entity.Domain.Models.Implements.SecurityAuthentication;
using Entity.DTOs.Implements.SecurityAuthentication.Auth;
using global::Business.CustomJWT;
using global::Business.Interfaces.Implements.SecurityAuthentication.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

/// <summary>
/// Implementación de <see cref="IAccessTokenFactory"/> basada en JWT (HMAC-SHA256).
/// </summary>
public sealed class JwtAccessTokenFactory : IAccessTokenFactory
{
    private readonly JwtSettings _settings;
    private readonly IClock _clock;

    public JwtAccessTokenFactory(IOptions<JwtSettings> settings, IClock clock)
    {
        _settings = settings.Value;
        _clock = clock;
    }

    public string Create(UserAuthDto user)
    {
        var now = _clock.UtcNow;
        var exp = now.AddMinutes(_settings.AccessTokenExpirationMinutes);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
        };

        if (user.PersonId.HasValue)
            claims.Add(new Claim("person_id", user.PersonId.Value.ToString()));

        foreach (var role in user.Roles.Distinct())
            claims.Add(new Claim(ClaimTypes.Role, role));

        var jwt = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: exp,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
