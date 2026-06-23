using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Projects;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Organize;

// Add / rename / delete the categories and sub-category rows of a project's Organize structure.
// Every command is scoped to the caller's organization (via the owning project) and returns the
// refreshed ProjectDetailDto so the frontend can re-render from a single response.

public record CreateCategoryCommand(Guid ProjectId, string Name) : IRequest<ProjectDetailDto>;
public record RenameCategoryCommand(Guid ProjectId, Guid CategoryId, string Name) : IRequest<ProjectDetailDto>;
public record DeleteCategoryCommand(Guid ProjectId, Guid CategoryId) : IRequest<ProjectDetailDto>;

public record CreateRequirementCommand(Guid ProjectId, Guid CategoryId, string Name) : IRequest<ProjectDetailDto>;
public record RenameRequirementCommand(Guid ProjectId, Guid RequirementId, string Name) : IRequest<ProjectDetailDto>;
public record DeleteRequirementCommand(Guid ProjectId, Guid RequirementId) : IRequest<ProjectDetailDto>;

internal static class OrganizeStructure
{
    public static string CleanName(string? name)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ValidationException(new Dictionary<string, string[]> { ["name"] = new[] { "Name is required." } });
        return trimmed.Length > 150 ? trimmed[..150] : trimmed;
    }

    public static async Task EnsureProjectAsync(IAppDbContext db, ICurrentUser current, Guid projectId, CancellationToken ct)
    {
        var ok = await db.Projects.AnyAsync(
            p => p.Id == projectId && p.OrganizationId == current.OrganizationId && !p.IsDeleted, ct);
        if (!ok) throw new NotFoundException("Project", projectId);
    }
}

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current;
    private readonly IDateTime _clock; private readonly IMediator _mediator;
    public CreateCategoryHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IMediator mediator)
        => (_db, _current, _clock, _mediator) = (db, current, clock, mediator);

    public async Task<ProjectDetailDto> Handle(CreateCategoryCommand r, CancellationToken ct)
    {
        await OrganizeStructure.EnsureProjectAsync(_db, _current, r.ProjectId, ct);
        var maxSort = await _db.ProjectRequirementCategories
            .Where(c => c.ProjectId == r.ProjectId).Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? -1;
        _db.ProjectRequirementCategories.Add(new ProjectRequirementCategory
        {
            ProjectId = r.ProjectId, Name = OrganizeStructure.CleanName(r.Name),
            SortOrder = maxSort + 1, CreatedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetProjectQuery(r.ProjectId), ct);
    }
}

public class RenameCategoryHandler : IRequestHandler<RenameCategoryCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current; private readonly IMediator _mediator;
    public RenameCategoryHandler(IAppDbContext db, ICurrentUser current, IMediator mediator)
        => (_db, _current, _mediator) = (db, current, mediator);

    public async Task<ProjectDetailDto> Handle(RenameCategoryCommand r, CancellationToken ct)
    {
        await OrganizeStructure.EnsureProjectAsync(_db, _current, r.ProjectId, ct);
        var category = await _db.ProjectRequirementCategories
            .FirstOrDefaultAsync(c => c.Id == r.CategoryId && c.ProjectId == r.ProjectId, ct)
            ?? throw new NotFoundException("Category", r.CategoryId);
        category.Name = OrganizeStructure.CleanName(r.Name);
        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetProjectQuery(r.ProjectId), ct);
    }
}

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current;
    private readonly IDateTime _clock; private readonly IMediator _mediator;
    public DeleteCategoryHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IMediator mediator)
        => (_db, _current, _clock, _mediator) = (db, current, clock, mediator);

    public async Task<ProjectDetailDto> Handle(DeleteCategoryCommand r, CancellationToken ct)
    {
        await OrganizeStructure.EnsureProjectAsync(_db, _current, r.ProjectId, ct);
        var category = await _db.ProjectRequirementCategories
            .Include(c => c.Requirements)
            .FirstOrDefaultAsync(c => c.Id == r.CategoryId && c.ProjectId == r.ProjectId, ct)
            ?? throw new NotFoundException("Category", r.CategoryId);

        var now = _clock.UtcNow;
        var rowIds = category.Requirements.Select(x => x.Id).ToList();

        // Soft-delete cascades nothing, so unmap any documents on this category's rows by hand
        // (otherwise they'd point at a hidden row and vanish from the workspace).
        if (rowIds.Count > 0)
        {
            var affected = await _db.ProjectDocumentAssignments
                .Where(a => a.ProjectId == r.ProjectId && a.RequirementId != null && rowIds.Contains(a.RequirementId.Value))
                .ToListAsync(ct);
            foreach (var a in affected) a.RequirementId = null;
        }
        foreach (var row in category.Requirements) { row.IsDeleted = true; row.DeletedAt = now; }
        category.IsDeleted = true; category.DeletedAt = now;

        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetProjectQuery(r.ProjectId), ct);
    }
}

public class CreateRequirementHandler : IRequestHandler<CreateRequirementCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current;
    private readonly IDateTime _clock; private readonly IMediator _mediator;
    public CreateRequirementHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IMediator mediator)
        => (_db, _current, _clock, _mediator) = (db, current, clock, mediator);

    public async Task<ProjectDetailDto> Handle(CreateRequirementCommand r, CancellationToken ct)
    {
        await OrganizeStructure.EnsureProjectAsync(_db, _current, r.ProjectId, ct);
        var categoryExists = await _db.ProjectRequirementCategories
            .AnyAsync(c => c.Id == r.CategoryId && c.ProjectId == r.ProjectId, ct);
        if (!categoryExists) throw new NotFoundException("Category", r.CategoryId);

        var maxSort = await _db.ProjectRequirements
            .Where(x => x.CategoryId == r.CategoryId).Select(x => (int?)x.SortOrder).MaxAsync(ct) ?? -1;
        _db.ProjectRequirements.Add(new ProjectRequirement
        {
            ProjectId = r.ProjectId, CategoryId = r.CategoryId, Name = OrganizeStructure.CleanName(r.Name),
            SortOrder = maxSort + 1, CreatedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetProjectQuery(r.ProjectId), ct);
    }
}

public class RenameRequirementHandler : IRequestHandler<RenameRequirementCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current; private readonly IMediator _mediator;
    public RenameRequirementHandler(IAppDbContext db, ICurrentUser current, IMediator mediator)
        => (_db, _current, _mediator) = (db, current, mediator);

    public async Task<ProjectDetailDto> Handle(RenameRequirementCommand r, CancellationToken ct)
    {
        await OrganizeStructure.EnsureProjectAsync(_db, _current, r.ProjectId, ct);
        var row = await _db.ProjectRequirements
            .FirstOrDefaultAsync(x => x.Id == r.RequirementId && x.ProjectId == r.ProjectId, ct)
            ?? throw new NotFoundException("Requirement", r.RequirementId);
        row.Name = OrganizeStructure.CleanName(r.Name);
        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetProjectQuery(r.ProjectId), ct);
    }
}

public class DeleteRequirementHandler : IRequestHandler<DeleteRequirementCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db; private readonly ICurrentUser _current;
    private readonly IDateTime _clock; private readonly IMediator _mediator;
    public DeleteRequirementHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IMediator mediator)
        => (_db, _current, _clock, _mediator) = (db, current, clock, mediator);

    public async Task<ProjectDetailDto> Handle(DeleteRequirementCommand r, CancellationToken ct)
    {
        await OrganizeStructure.EnsureProjectAsync(_db, _current, r.ProjectId, ct);
        var row = await _db.ProjectRequirements
            .FirstOrDefaultAsync(x => x.Id == r.RequirementId && x.ProjectId == r.ProjectId, ct)
            ?? throw new NotFoundException("Requirement", r.RequirementId);

        var affected = await _db.ProjectDocumentAssignments
            .Where(a => a.ProjectId == r.ProjectId && a.RequirementId == r.RequirementId)
            .ToListAsync(ct);
        foreach (var a in affected) a.RequirementId = null;

        row.IsDeleted = true; row.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await _mediator.Send(new GetProjectQuery(r.ProjectId), ct);
    }
}
