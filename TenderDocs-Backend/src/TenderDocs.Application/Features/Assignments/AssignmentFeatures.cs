using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Assignments;

// ----- Add an existing document to a project ("Add Existing") -----
public record AddDocumentToProjectCommand(Guid ProjectId, Guid DocumentId, Guid? RequirementId) : IRequest;

public class AddDocumentToProjectHandler : IRequestHandler<AddDocumentToProjectCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public AddDocumentToProjectHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task Handle(AddDocumentToProjectCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var projectExists = await _db.Projects.AnyAsync(p => p.Id == r.ProjectId && p.OrganizationId == orgId && !p.IsDeleted, ct);
        if (!projectExists) throw new NotFoundException("Project", r.ProjectId);
        var docExists = await _db.Documents.AnyAsync(d => d.Id == r.DocumentId && d.OrganizationId == orgId && !d.IsDeleted, ct);
        if (!docExists) throw new NotFoundException("Document", r.DocumentId);

        // Normalize + validate the requirement slot. A document belongs to at most ONE
        // requirement category per project, so we keep a single assignment per (project, doc)
        // and just move its RequirementId. requirementId == null means "in project, unmapped".
        Guid? requirementId = r.RequirementId;
        if (requirementId == Guid.Empty)
            requirementId = null;
        if (requirementId is not null)
        {
            var validRequirement = await _db.ProjectRequirements
                .AnyAsync(req => req.Id == requirementId && req.ProjectId == r.ProjectId, ct);
            if (!validRequirement) requirementId = null;
        }

        var existing = await _db.ProjectDocumentAssignments
            .FirstOrDefaultAsync(a => a.ProjectId == r.ProjectId && a.DocumentId == r.DocumentId, ct);

        if (existing is not null)
        {
            // Already in the project — this is a MOVE between requirement rows (or to/from Unmapped).
            existing.RequirementId = requirementId;
        }
        else
        {
            _db.ProjectDocumentAssignments.Add(new ProjectDocumentAssignment
            {
                ProjectId = r.ProjectId, DocumentId = r.DocumentId, RequirementId = requirementId,
                AssignedById = _current.UserId, CreatedAt = _clock.UtcNow
            });
        }
        await _db.SaveChangesAsync(ct);
    }
}

// ----- Remove a document from a project -----
public record RemoveDocumentFromProjectCommand(Guid ProjectId, Guid DocumentId) : IRequest;

public class RemoveDocumentFromProjectHandler : IRequestHandler<RemoveDocumentFromProjectCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public RemoveDocumentFromProjectHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task Handle(RemoveDocumentFromProjectCommand r, CancellationToken ct)
    {
        var a = await _db.ProjectDocumentAssignments
            .FirstOrDefaultAsync(x => x.ProjectId == r.ProjectId && x.DocumentId == r.DocumentId, ct)
            ?? throw new NotFoundException("Assignment", $"{r.ProjectId}/{r.DocumentId}");
        _db.ProjectDocumentAssignments.Remove(a);
        await _db.SaveChangesAsync(ct);
    }
}
