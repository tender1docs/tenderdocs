using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Auth;

/// <summary>Google OAuth login/signup. Accepts either an auth code (web flow) or an id_token.</summary>
public record GoogleLoginCommand(string? Code, string? IdToken, string? RedirectUri, string? Ip)
    : IRequest<AuthResultDto>;

public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IGoogleOAuthService _google;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTime _clock;

    public GoogleLoginHandler(IAppDbContext db, IGoogleOAuthService google, IJwtTokenService jwt, IDateTime clock)
        => (_db, _google, _jwt, _clock) = (db, google, jwt, clock);

    public async Task<AuthResultDto> Handle(GoogleLoginCommand r, CancellationToken ct)
    {
        var info = !string.IsNullOrWhiteSpace(r.Code)
            ? await _google.ExchangeCodeAsync(r.Code!, r.RedirectUri ?? "", ct)
            : await _google.ValidateIdTokenAsync(r.IdToken ?? "", ct);

        var email = info.Email.ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email || u.GoogleId == info.GoogleId, ct);

        if (user is null)
        {
            var org = new Organization
            {
                Name = $"{info.FullName}'s Workspace", Slug = Guid.NewGuid().ToString("N")[..10],
                DemoMode = true, CreatedAt = _clock.UtcNow
            };
            _db.Organizations.Add(org);
            user = new User
            {
                OrganizationId = org.Id, Email = email, GoogleId = info.GoogleId,
                FullName = info.FullName, Initials = RegisterHandler.Initials(info.FullName),
                // New Google users start as Viewer; they pick their role right after sign-in.
                Role = UserRole.Viewer, CreatedAt = _clock.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
            user.Organization = org;
        }
        else if (user.GoogleId is null)
        {
            user.GoogleId = info.GoogleId; // link existing email account
        }

        user.LastLoginAt = _clock.UtcNow;
        var (token, exp) = _jwt.CreateAccessToken(user);
        var refresh = new RefreshToken
        {
            UserId = user.Id, Token = _jwt.CreateRefreshToken(),
            ExpiresAt = _clock.UtcNow.AddDays(14), CreatedAt = _clock.UtcNow, CreatedByIp = r.Ip
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        return new AuthResultDto(token, exp, refresh.Token,
            new UserDto(user.Id, user.Email, user.FullName, user.Initials, user.Role.ToString(),
                user.OrganizationId, user.Organization.Name, user.Organization.DemoMode));
    }
}
