using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Infrastructure.Identity;

/// <summary>
/// Issues signed JWT access tokens and cryptographically-random refresh tokens.
/// Reads Jwt:Secret / Issuer / Audience / AccessTokenMinutes from configuration.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _config;
    private readonly IDateTime _clock;

    public JwtTokenService(IConfiguration config, IDateTime clock)
        => (_config, _clock) = (config, clock);

    public (string token, DateTimeOffset expiresAt) CreateAccessToken(User user)
    {
        var section = _config.GetSection("Jwt");
        var secret = section["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var minutes = int.TryParse(section["AccessTokenMinutes"], out var m) ? m : 60;

        // Derive a stable 32-byte key from the configured secret so HS256 works regardless of the
        // secret length (HS256 requires >= 256-bit keys). Backward-compatible with any prior secret.
        var key = new SymmetricSecurityKey(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = _clock.UtcNow.AddMinutes(minutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("org", user.OrganizationId.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: creds);

        var encoded = new JwtSecurityTokenHandler().WriteToken(token);
        return (encoded, expires);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
