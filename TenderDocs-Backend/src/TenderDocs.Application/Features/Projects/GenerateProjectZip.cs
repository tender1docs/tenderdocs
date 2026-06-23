using System.IO.Compression;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Documents;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Application.Features.Projects;

/// <summary>
/// Builds a ZIP for a project. Files are grouped into one folder per Organize category, and each
/// file name is tagged with its sub-category (row):
///   ProjectName/Financial/&lt;stem&gt;_GST.pdf   ProjectName/Technical/&lt;stem&gt;_TechnicalDocuments.pdf
/// Documents not mapped to a row land in ProjectName/Others/. Folder and tag names come from the
/// per-project, renamable category/row structure, so renames and added categories appear here.
///
/// The archive is assembled fully in memory inside the request scope (where the DbContext and
/// storage providers are guaranteed alive) and returned as bytes. This is provider-agnostic
/// (Local, Google Drive, future providers all expose DownloadFileAsync) and avoids the pitfalls
/// of streaming a ZIP after the handler returns. A file that can't be read is skipped rather than
/// failing the whole export.
/// </summary>
public record GenerateProjectZipQuery(Guid ProjectId) : IRequest<ProjectZipDto>;
public record ProjectZipDto(byte[] Content, string FileName);

public class GenerateProjectZipHandler : IRequestHandler<GenerateProjectZipQuery, ProjectZipDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IStorageProviderFactory _factory;
    private readonly ILogger<GenerateProjectZipHandler> _logger;
    public GenerateProjectZipHandler(IAppDbContext db, ICurrentUser current, IStorageProviderFactory factory,
        ILogger<GenerateProjectZipHandler> logger)
        => (_db, _current, _factory, _logger) = (db, current, factory, logger);

    public async Task<ProjectZipDto> Handle(GenerateProjectZipQuery q, CancellationToken ct)
    {
        var project = await _db.Projects
            .Include(p => p.RequirementCategories)
            .Include(p => p.Assignments).ThenInclude(a => a.Document)
            .Include(p => p.Assignments).ThenInclude(a => a.Requirement).ThenInclude(r => r!.Category)
            .FirstOrDefaultAsync(p => p.Id == q.ProjectId && p.OrganizationId == _current.OrganizationId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Project", q.ProjectId);

        const string othersFolder = "Others";
        var safeName = SafeName(project.Name);
        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            // Always create one folder per category (plus Others), even if empty.
            // (A trailing-slash entry is how a ZIP represents an empty directory.)
            foreach (var category in project.RequirementCategories.OrderBy(c => c.SortOrder))
                archive.CreateEntry($"{safeName}/{Segment(category.Name)}/");
            archive.CreateEntry($"{safeName}/{othersFolder}/");

            foreach (var assignment in project.Assignments)
            {
                ct.ThrowIfCancellationRequested();
                var d = assignment.Document;
                if (d is null || d.IsDeleted) continue;

                // Folder = the row's category name; filename tag = the row name. Unmapped → Others.
                var row = assignment.Requirement;
                var folder = row?.Category is { } cat ? Segment(cat.Name) : othersFolder;
                var tag = row is not null ? Tag(row.Name) : othersFolder;

                var rawName = string.IsNullOrWhiteSpace(d.Name) ? $"{d.Id}" : d.Name;
                var ext = Path.GetExtension(rawName);
                var stem = Path.GetFileNameWithoutExtension(rawName);
                var fileName = $"{stem}_{tag}{ext}";

                var path = $"{safeName}/{folder}/{fileName}";
                var n = 1;
                while (!seenPaths.Add(path))
                    path = $"{safeName}/{folder}/{stem}_{tag} ({n++}){ext}";

                try
                {
                    var provider = _factory.GetProvider(d.StorageProvider);
                    await using var src = await provider.DownloadFileAsync(d.StorageKey, ct);
                    var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await src.CopyToAsync(entryStream, ct);
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    // Skip files that can't be read (missing on disk, revoked Drive token, etc.)
                    // so one bad file doesn't break the whole bundle.
                    _logger.LogWarning(ex,
                        "Skipping document {DocumentId} ({StorageProvider}/{StorageKey}) in ZIP for project {ProjectId}: {Reason}",
                        d.Id, d.StorageProvider, d.StorageKey, project.Id, ex.Message);
                }
            }
        }

        var docCount = project.Assignments.Count(a => a.Document is not null && !a.Document.IsDeleted);

        _logger.LogInformation("Generated ZIP for project {ProjectId} with {DocumentCount} document(s).",
            project.Id, docCount);

        return new ProjectZipDto(buffer.ToArray(), $"{safeName}.zip");
    }

    private static string SafeName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        return name.Trim().Length == 0 ? "Project" : name.Trim();
    }

    /// <summary>A category name made safe for use as a folder segment (keeps spaces).</summary>
    private static string Segment(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
        name = name.Trim();
        return name.Length == 0 ? "Untitled" : name;
    }

    /// <summary>A row name compacted into a filename tag, e.g. "IT Returns" -> "ITReturns".</summary>
    private static string Tag(string name)
    {
        var seg = Segment(name).Replace(" ", string.Empty);
        return seg.Length == 0 ? "Doc" : seg;
    }
}
