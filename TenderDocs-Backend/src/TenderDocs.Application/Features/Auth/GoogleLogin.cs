using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Auth;

/// <summary>
/// Google OAuth sign-in for a provisioned user. Accepts either an auth code (web flow) or an id_token.
/// Access is controlled: only an existing, active account (matched by email or Google id) may sign in —
/// there is no self-registration and no auto-Viewer fallback. An administrator provisions users first
/// (Administration → Users); the user's stored role determines their permissions.
/// </summary>
public record GoogleLoginCommand(string? Code, string? IdToken, string? RedirectUri, string? Ip)
    : IRequest<AuthResultDto>;

public class GoogleLoginHandler : IRequestHandler<GoogleLoginCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IGoogleOAuthService _google;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;

    public GoogleLoginHandler(IAppDbContext db, IGoogleOAuthService google, IJwtTokenService jwt,
        IDateTime clock, IAuditLogger audit)
        => (_db, _google, _jwt, _clock, _audit) = (db, google, jwt, clock, audit);

    public async Task<AuthResultDto> Handle(GoogleLoginCommand r, CancellationToken ct)
    {
        var info = !string.IsNullOrWhiteSpace(r.Code)
            ? await _google.ExchangeCodeAsync(r.Code!, r.RedirectUri ?? "", ct)
            : await _google.ValidateIdTokenAsync(r.IdToken ?? "", ct);

        var email = info.Email.ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email || u.GoogleId == info.GoogleId, ct);

        // Controlled access — no provisioned account means no entry (no self-registration / auto-Viewer).
        if (user is null)
            throw new ForbiddenAccessException(
                "This Google account isn't authorized. Ask your administrator to add you.");
        if (!user.IsActive)
            throw new ForbiddenAccessException("This account is disabled. Contact your administrator.");

        if (user.GoogleId is null)
            user.GoogleId = info.GoogleId;   // link the Google identity to the pre-provisioned account

        user.LastLoginAt = _clock.UtcNow;
        var (token, exp) = _jwt.CreateAccessToken(user);
        var refresh = new RefreshToken
        {
            UserId = user.Id, Token = _jwt.CreateRefreshToken(),
            ExpiresAt = _clock.UtcNow.AddDays(14), CreatedAt = _clock.UtcNow, CreatedByIp = r.Ip
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Login, "User", user.Id, new { method = "google" },
            user.OrganizationId, user.Id, r.Ip, ct);

        return new AuthResultDto(token, exp, refresh.Token,
            UserDto.From(user, user.Organization.Name, user.Organization.DemoMode));
    }
}
