using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Auth;

/// <summary>
/// Lets the signed-in user self-assign one of the three access roles (Approver / Uploader / Viewer)
/// so every role can be tried from a single test account. Issues a fresh token carrying the new role
/// claim. (Self-service selection is intentional for this app; a production deployment would instead
/// have an approver/admin assign roles.)
/// </summary>
public record SelectRoleCommand(UserRole Role, string? Ip) : IRequest<AuthResultDto>;

public class SelectRoleHandler : IRequestHandler<SelectRoleCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTime _clock;

    public SelectRoleHandler(IAppDbContext db, ICurrentUser current, IJwtTokenService jwt, IDateTime clock)
        => (_db, _current, _jwt, _clock) = (db, current, jwt, clock);

    public async Task<AuthResultDto> Handle(SelectRoleCommand r, CancellationToken ct)
    {
        var userId = _current.UserId ?? throw new ForbiddenAccessException();
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("User", userId);

        user.Role = r.Role;   // set before issuing the token so the new role claim is carried

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
