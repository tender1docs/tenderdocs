using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Users;

/// <summary>Admin: change a user's role (and therefore their permission set).</summary>
public record UpdateUserRoleCommand(Guid UserId, UserRole Role) : IRequest<TeamMemberDto>;

public class UpdateUserRoleHandler : IRequestHandler<UpdateUserRoleCommand, TeamMemberDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IAuditLogger _audit;

    public UpdateUserRoleHandler(IAppDbContext db, ICurrentUser current, IAuditLogger audit)
        => (_db, _current, _audit) = (db, current, audit);

    public async Task<TeamMemberDto> Handle(UpdateUserRoleCommand r, CancellationToken ct)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == r.UserId
                && u.OrganizationId == _current.OrganizationId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("User", r.UserId);

        var previous = user.Role;

        // Don't allow demoting the last remaining Admin — it would lock everyone out of administration.
        if (previous == UserRole.Admin && r.Role != UserRole.Admin && await IsLastAdmin(user.Id, user.OrganizationId, ct))
            throw new ConflictException("Cannot change the role of the last administrator.");

        user.Role = r.Role;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, "User", user.Id,
            new { user.Email, from = previous.ToString(), to = r.Role.ToString(), change = "role" }, ct: ct);

        return new TeamMemberDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Initials, user.IsActive);
    }

    private async Task<bool> IsLastAdmin(Guid userId, Guid orgId, CancellationToken ct) =>
        !await _db.Users.AnyAsync(u => u.OrganizationId == orgId && u.Role == UserRole.Admin
            && u.Id != userId && u.IsActive && !u.IsDeleted, ct);
}
