using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Users;

/// <summary>Admin: soft-delete a user (they can no longer sign in or appear in lists).</summary>
public record DeleteUserCommand(Guid UserId) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;
    public DeleteUserHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IAuditLogger audit)
        => (_db, _current, _clock, _audit) = (db, current, clock, audit);

    public async Task Handle(DeleteUserCommand r, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == r.UserId && u.OrganizationId == _current.OrganizationId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("User", r.UserId);

        if (user.Id == _current.UserId)
            throw new ConflictException("You cannot delete your own account.");

        if (user.Role == UserRole.Admin)
        {
            var otherAdmins = await _db.Users.CountAsync(u => u.OrganizationId == user.OrganizationId
                && u.Role == UserRole.Admin && u.Id != user.Id && u.IsActive && !u.IsDeleted, ct);
            if (otherAdmins == 0) throw new ConflictException("Cannot delete the last administrator.");
        }

        user.IsDeleted = true;
        user.DeletedAt = _clock.UtcNow;
        user.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Delete, "User", user.Id, new { user.Email }, ct: ct);
    }
}
