using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Documents;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Projects;

/// <summary>
/// The default two-level Organize structure (categories → sub-category rows) and the logic to seed
/// it into a project. Shared by <see cref="CreateProjectHandler"/> and
/// <see cref="EnsureProjectRequirementsHandler"/> so both paths produce the same starting layout.
/// Row names intentionally mirror <see cref="DocumentMapping.Label"/> so existing documents can be
/// auto-placed into the matching row by their <c>DocumentType</c>.
/// </summary>
public static class OrganizeDefaults
{
    public static readonly (string Category, string[] Rows)[] Structure =
    {
        ("Financial", new[] { "GST", "PAN", "IT Returns", "MSME", "ISO", "Bank Statements", "Financial Documents" }),
        ("Technical", new[] { "Experience Certificates", "Technical Documents" }),
    };

    /// <summary>
    /// Seeds the default categories + rows into <paramref name="project"/> when it has none yet.
    /// Adopts pre-existing flat requirements (CategoryId == null) by name so their assignments
    /// survive, and soft-deletes leftover orphans (unmapping their documents). Idempotent.
    ///
    /// Does NOT map documents to rows — call <see cref="AutoMapDocumentsByType"/> in a *separate*
    /// SaveChanges once the new rows have persisted IDs (mapping an assignment to an unsaved row in
    /// the same save trips the assignment→requirement FK on insert ordering).
    ///
    /// The project must be loaded with <c>RequirementCategories</c>, <c>Requirements</c> and
    /// <c>Assignments</c> for the adoption step to work. New rows/categories are explicitly added to
    /// <paramref name="db"/> — entities carry a client-set Guid key, so without an explicit Add EF
    /// would treat them as existing (and emit UPDATEs that affect no rows) when discovered only via
    /// a navigation on the already-tracked project.
    /// </summary>
    public static void SeedStructure(IAppDbContext db, Project project, IDateTime clock)
    {
        if (project.RequirementCategories.Any(c => !c.IsDeleted)) return;

        var now = clock.UtcNow;
        var orphans = project.Requirements.Where(r => r.CategoryId == null && !r.IsDeleted).ToList();
        var adopted = new HashSet<Guid>();

        var catOrder = 0;
        foreach (var (categoryName, rows) in Structure)
        {
            var category = new ProjectRequirementCategory { ProjectId = project.Id, Name = categoryName, SortOrder = catOrder++, CreatedAt = now };
            project.RequirementCategories.Add(category);
            db.ProjectRequirementCategories.Add(category);

            var rowOrder = 0;
            foreach (var rowName in rows)
            {
                // Reuse a same-named orphan row so any assignments pointing at it are preserved.
                var existing = orphans.FirstOrDefault(o => !adopted.Contains(o.Id)
                    && string.Equals(o.Name, rowName, StringComparison.OrdinalIgnoreCase));
                if (existing is not null)
                {
                    existing.Category = category;
                    existing.SortOrder = rowOrder++;
                    adopted.Add(existing.Id);
                }
                else
                {
                    // Set ProjectId explicitly: the row is linked to the project only via its category,
                    // so EF won't infer the (separate) ProjectRequirement→Project FK on its own.
                    var row = new ProjectRequirement
                    {
                        ProjectId = project.Id, Name = rowName, Category = category, SortOrder = rowOrder++, CreatedAt = now,
                    };
                    category.Requirements.Add(row);
                    db.ProjectRequirements.Add(row);
                }
            }
        }

        // Drop leftover orphan rows (e.g. the old generic "Financial"/"Technical"/"Others") and
        // unmap any documents that were on them so they fall back to the Others bucket.
        foreach (var orphan in orphans.Where(o => !adopted.Contains(o.Id)))
        {
            orphan.IsDeleted = true;
            orphan.DeletedAt = now;
            foreach (var a in project.Assignments.Where(a => a.RequirementId == orphan.Id))
                a.RequirementId = null;
        }
    }

    /// <summary>
    /// Best-effort: places currently-unmapped project documents into the (already-persisted) row
    /// whose name matches the document's type label, preserving the grouping users saw before this
    /// change. Run after <see cref="SeedStructure"/> + SaveChanges so the rows have real IDs.
    /// Requires <c>RequirementCategories.Requirements</c> and <c>Assignments.Document</c> loaded.
    /// </summary>
    public static void AutoMapDocumentsByType(Project project)
    {
        var rowByName = project.RequirementCategories
            .Where(c => !c.IsDeleted)
            .SelectMany(c => c.Requirements.Where(r => !r.IsDeleted))
            .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var a in project.Assignments.Where(a => a.RequirementId == null))
        {
            if (a.Document is null) continue;
            var label = DocumentMapping.Label(a.Document.DocumentType);
            if (rowByName.TryGetValue(label, out var row))
                a.RequirementId = row.Id;
        }
    }
}
