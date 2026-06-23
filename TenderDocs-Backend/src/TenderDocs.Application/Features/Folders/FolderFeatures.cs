using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Folders;

public record FolderNodeDto(Guid Id, string Name, Guid? ParentFolderId, int Depth,
    string MaterializedPath, int DocumentCount, List<FolderNodeDto> Children);

// ----- Create folder (supports unlimited nesting) -----
public record CreateFolderCommand(string Name, Guid? ParentFolderId, Guid? ProjectId) : IRequest<FolderNodeDto>;

public class CreateFolderValidator : AbstractValidator<CreateFolderCommand>
{
    public CreateFolderValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
}

public class CreateFolderHandler : IRequestHandler<CreateFolderCommand, FolderNodeDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public CreateFolderHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task<FolderNodeDto> Handle(CreateFolderCommand r, CancellationToken ct)
    {
        Folder? parent = null;
        if (r.ParentFolderId is not null)
            parent = await _db.Folders.FirstOrDefaultAsync(
                f => f.Id == r.ParentFolderId && f.OrganizationId == _current.OrganizationId, ct)
                ?? throw new NotFoundException("Folder", r.ParentFolderId);

        var folder = new Folder
        {
            OrganizationId = _current.OrganizationId!.Value,
            ProjectId = r.ProjectId ?? parent?.ProjectId,
            ParentFolderId = parent?.Id,
            Name = r.Name.Trim(),
            Depth = parent is null ? 0 : parent.Depth + 1,
            CreatedAt = _clock.UtcNow
        };
        // Materialized path: parent path + this id + "/"
        var basePath = parent?.MaterializedPath ?? "/";
        folder.MaterializedPath = $"{basePath}{folder.Id:N}/";
        _db.Folders.Add(folder);
        await _db.SaveChangesAsync(ct);

        return new FolderNodeDto(folder.Id, folder.Name, folder.ParentFolderId, folder.Depth,
            folder.MaterializedPath, 0, new());
    }
}

// ----- Get full tree (fast: single query, built in memory from materialized path) -----
public record GetFolderTreeQuery(Guid? ProjectId) : IRequest<IReadOnlyList<FolderNodeDto>>;

public class GetFolderTreeHandler : IRequestHandler<GetFolderTreeQuery, IReadOnlyList<FolderNodeDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GetFolderTreeHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<FolderNodeDto>> Handle(GetFolderTreeQuery q, CancellationToken ct)
    {
        var folders = await _db.Folders
            .Where(f => f.OrganizationId == _current.OrganizationId
                        && !f.IsDeleted
                        && (q.ProjectId == null || f.ProjectId == q.ProjectId))
            .OrderBy(f => f.MaterializedPath)
            .Select(f => new { f.Id, f.Name, f.ParentFolderId, f.Depth, f.MaterializedPath,
                DocumentCount = f.Documents.Count(d => !d.IsDeleted) })
            .ToListAsync(ct);

        var nodes = folders.ToDictionary(f => f.Id,
            f => new FolderNodeDto(f.Id, f.Name, f.ParentFolderId, f.Depth, f.MaterializedPath, f.DocumentCount, new()));

        var roots = new List<FolderNodeDto>();
        foreach (var n in nodes.Values.OrderBy(n => n.MaterializedPath))
        {
            if (n.ParentFolderId is not null && nodes.TryGetValue(n.ParentFolderId.Value, out var parent))
                parent.Children.Add(n);
            else roots.Add(n);
        }
        return roots;
    }
}

// ----- Move folder (re-parent + rewrite subtree materialized paths) -----
public record MoveFolderCommand(Guid Id, Guid? NewParentFolderId) : IRequest;

public class MoveFolderHandler : IRequestHandler<MoveFolderCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public MoveFolderHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task Handle(MoveFolderCommand r, CancellationToken ct)
    {
        var folder = await _db.Folders.FirstOrDefaultAsync(
            f => f.Id == r.Id && f.OrganizationId == _current.OrganizationId, ct)
            ?? throw new NotFoundException("Folder", r.Id);

        Folder? newParent = null;
        if (r.NewParentFolderId is not null)
        {
            newParent = await _db.Folders.FirstOrDefaultAsync(
                f => f.Id == r.NewParentFolderId && f.OrganizationId == _current.OrganizationId, ct)
                ?? throw new NotFoundException("Folder", r.NewParentFolderId);
            if (newParent.MaterializedPath.Contains(folder.Id.ToString("N")))
                throw new ConflictException("Cannot move a folder into its own descendant.");
        }

        var oldPath = folder.MaterializedPath;
        var newBase = newParent?.MaterializedPath ?? "/";
        var newPath = $"{newBase}{folder.Id:N}/";
        var depthDelta = (newParent?.Depth + 1 ?? 0) - folder.Depth;

        // rewrite this folder + all descendants (single load of subtree)
        var subtree = await _db.Folders
            .Where(f => f.OrganizationId == _current.OrganizationId && f.MaterializedPath.StartsWith(oldPath))
            .ToListAsync(ct);
        foreach (var f in subtree)
        {
            f.MaterializedPath = string.Concat(newPath, f.MaterializedPath.AsSpan(oldPath.Length));
            f.Depth += depthDelta;
            f.UpdatedAt = _clock.UtcNow;
        }
        folder.ParentFolderId = newParent?.Id;
        await _db.SaveChangesAsync(ct);
    }
}

// ----- Delete folder (soft) -----
public record DeleteFolderCommand(Guid Id) : IRequest;

public class DeleteFolderHandler : IRequestHandler<DeleteFolderCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public DeleteFolderHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task Handle(DeleteFolderCommand r, CancellationToken ct)
    {
        var folder = await _db.Folders.FirstOrDefaultAsync(
            f => f.Id == r.Id && f.OrganizationId == _current.OrganizationId, ct)
            ?? throw new NotFoundException("Folder", r.Id);
        var subtree = await _db.Folders
            .Where(f => f.OrganizationId == _current.OrganizationId && f.MaterializedPath.StartsWith(folder.MaterializedPath))
            .ToListAsync(ct);
        foreach (var f in subtree) { f.IsDeleted = true; f.DeletedAt = _clock.UtcNow; }
        await _db.SaveChangesAsync(ct);
    }
}
