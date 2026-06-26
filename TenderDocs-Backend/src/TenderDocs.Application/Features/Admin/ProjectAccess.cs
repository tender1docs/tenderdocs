using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Admin;

/// <summary>Shared helper for project-level access scoping.</summary>
public static class ProjectAccessScope
{
    /// <summary>Admins bypass project scoping and see everything in the organization.</summary>
    public static bool IsAdmin(ICurrentUser u) =>
        string.Equals(u.Role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);
}

/// <summary>Admin: the set of project IDs a user is assigned to.</summary>
public record GetUserProjectsQuery(Guid UserId) : IRequest<IReadOnlyList<Guid>>;

public class GetUserProjectsHandler : IRequestHandler<GetUserProjectsQuery, IReadOnlyList<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GetUserProjectsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<Guid>> Handle(GetUserProjectsQuery q, CancellationToken ct)
        => await _db.UserProjects
            .Where(up => up.UserId == q.UserId
                && _db.Projects.Any(p => p.Id == up.ProjectId && p.OrganizationId == _current.OrganizationId))
            .Select(up => up.ProjectId)
            .ToListAsync(ct);
}

/// <summary>Admin: replace the full set of projects a user may access.</summary>
public record SetUserProjectsCommand(Guid UserId, List<Guid> ProjectIds) : IRequest<IReadOnlyList<Guid>>;

public class SetUserProjectsHandler : IRequestHandler<SetUserProjectsCommand, IReadOnlyList<Guid>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;
    public SetUserProjectsHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IAuditLogger audit)
        => (_db, _current, _clock, _audit) = (db, current, clock, audit);

    public async Task<IReadOnlyList<Guid>> Handle(SetUserProjectsCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId!.Value;
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == r.UserId && u.OrganizationId == orgId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("User", r.UserId);

        var validIds = await _db.Projects
            .Where(p => p.OrganizationId == orgId && !p.IsDeleted && r.ProjectIds.Contains(p.Id))
            .Select(p => p.Id).ToListAsync(ct);

        var existing = await _db.UserProjects.Where(up => up.UserId == r.UserId).ToListAsync(ct);
        var existingIds = existing.Select(e => e.ProjectId).ToHashSet();

        foreach (var e in existing.Where(e => !validIds.Contains(e.ProjectId)))
            _db.UserProjects.Remove(e);                       // hard delete (no soft-delete interceptor)
        foreach (var pid in validIds.Where(id => !existingIds.Contains(id)))
            _db.UserProjects.Add(new UserProject { UserId = r.UserId, ProjectId = pid, CreatedAt = _clock.UtcNow });

        await _db.SaveChangesAsync(ct);
        await _audit.LogAsync(AuditAction.Assign, "User", r.UserId,
            new { user.Email, projects = validIds.Count }, ct: ct);
        return validIds;
    }
}
