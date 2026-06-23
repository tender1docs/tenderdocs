using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Projects;

/// <summary>
/// Replaces the entire set of documents assigned to a project to match the supplied list.
/// Backs the Organize workspace + project detail "set documents" interaction: it diffs the
/// requested IDs against the current assignments, removing and adding only what changed.
/// </summary>
public record SetProjectDocumentsCommand(Guid ProjectId, IReadOnlyList<Guid> DocumentIds)
    : IRequest<ProjectDetailDto>;

public class SetProjectDocumentsHandler : IRequestHandler<SetProjectDocumentsCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    private readonly IMediator _mediator;

    public SetProjectDocumentsHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IMediator mediator)
        => (_db, _current, _clock, _mediator) = (db, current, clock, mediator);

    public async Task<ProjectDetailDto> Handle(SetProjectDocumentsCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var project = await _db.Projects
            .Include(p => p.Assignments)
            .FirstOrDefaultAsync(p => p.Id == r.ProjectId && p.OrganizationId == orgId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Project", r.ProjectId);

        var requested = r.DocumentIds.Distinct().ToHashSet();

        // Only allow documents that belong to this organization.
        if (requested.Count > 0)
        {
            var validIds = await _db.Documents
                .Where(d => d.OrganizationId == orgId && !d.IsDeleted && requested.Contains(d.Id))
                .Select(d => d.Id)
                .ToListAsync(ct);
            requested = validIds.ToHashSet();
        }

        var existing = project.Assignments.ToList();
        var existingIds = existing.Select(a => a.DocumentId).ToHashSet();

        // Remove assignments no longer requested.
        foreach (var a in existing.Where(a => !requested.Contains(a.DocumentId)))
            _db.ProjectDocumentAssignments.Remove(a);

        // Add newly requested assignments.
        foreach (var docId in requested.Where(id => !existingIds.Contains(id)))
            _db.ProjectDocumentAssignments.Add(new ProjectDocumentAssignment
            {
                ProjectId = project.Id,
                DocumentId = docId,
                AssignedById = _current.UserId,
                CreatedAt = _clock.UtcNow
            });

        await _db.SaveChangesAsync(ct);

        // Return the refreshed detail so the client has up-to-date documents.
        return await _mediator.Send(new GetProjectQuery(project.Id), ct);
    }
}
