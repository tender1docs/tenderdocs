using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Users;

/// <summary>Admin: activate or deactivate a user. A deactivated user cannot sign in.</summary>
public record SetUserActiveCommand(Guid UserId, bool IsActive) : IRequest<TeamMemberDto>;

public class SetUserActiveHandler : IRequestHandler<SetUserActiveCommand, TeamMemberDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IAuditLogger _audit;

    public SetUserActiveHandler(IAppDbContext db, ICurrentUser current, IAuditLogger audit)
        => (_db, _current, _audit) = (db, current, audit);

    public async Task<TeamMemberDto> Handle(SetUserActiveCommand r, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == r.UserId
                && u.OrganizationId == _current.OrganizationId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("User", r.UserId);

        if (!r.IsActive)
        {
            if (user.Id == _current.UserId)
                throw new ConflictException("You cannot deactivate your own account.");

            if (user.Role == UserRole.Admin && await IsLastAdmin(user.Id, user.OrganizationId, ct))
                throw new ConflictException("Cannot deactivate the last administrator.");
        }

        user.IsActive = r.IsActive;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, "User", user.Id,
            new { user.Email, isActive = r.IsActive, change = "status" }, ct: ct);

        return new TeamMemberDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Initials, user.IsActive);
    }

    private async Task<bool> IsLastAdmin(Guid userId, Guid orgId, CancellationToken ct) =>
        !await _db.Users.AnyAsync(u => u.OrganizationId == orgId && u.Role == UserRole.Admin
            && u.Id != userId && u.IsActive && !u.IsDeleted, ct);
}
