using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Auth;

public record RefreshTokenCommand(string RefreshToken, string? Ip) : IRequest<AuthResultDto>;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTime _clock;

    public RefreshTokenHandler(IAppDbContext db, IJwtTokenService jwt, IDateTime clock)
        => (_db, _jwt, _clock) = (db, jwt, clock);

    public async Task<AuthResultDto> Handle(RefreshTokenCommand r, CancellationToken ct)
    {
        var existing = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == r.RefreshToken, ct)
            ?? throw new ForbiddenAccessException("Invalid refresh token.");
        if (!existing.IsActive) throw new ForbiddenAccessException("Refresh token expired or revoked.");

        var user = await _db.Users.Include(u => u.Organization)
            .FirstAsync(u => u.Id == existing.UserId, ct);

        // rotate
        existing.RevokedAt = _clock.UtcNow;
        var newRefresh = new RefreshToken
        {
            UserId = user.Id, Token = _jwt.CreateRefreshToken(),
            ExpiresAt = _clock.UtcNow.AddDays(14), CreatedAt = _clock.UtcNow, CreatedByIp = r.Ip
        };
        existing.ReplacedByToken = newRefresh.Token;
        _db.RefreshTokens.Add(newRefresh);

        var (token, exp) = _jwt.CreateAccessToken(user);
        await _db.SaveChangesAsync(ct);

        return new AuthResultDto(token, exp, newRefresh.Token,
            UserDto.From(user, user.Organization.Name, user.Organization.DemoMode));
    }
}
