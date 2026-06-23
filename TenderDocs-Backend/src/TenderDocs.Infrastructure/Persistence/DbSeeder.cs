using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Infrastructure.Persistence;

/// <summary>
/// Seeds a demo organization, an Admin user, a Google-Drive-style folder tree, sample
/// documents (with real files written to the active storage provider so download + ZIP work),
/// sample projects, assignments, and notifications. Runs once on startup; no-op if any user exists.
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IStorageProviderFactory _storageFactory;
    private readonly IDateTime _clock;
    private readonly ILogger<DbSeeder> _logger;

    public const string AdminEmail = "admin@tenderdocs.io";
    public const string AdminPassword = "Admin@12345";
    public const string ReviewerEmail = "reviewer@tenderdocs.io";
    public const string ReviewerPassword = "Reviewer@12345";
    public const string ViewerEmail = "viewer@tenderdocs.io";
    public const string ViewerPassword = "Viewer@12345";

    public DbSeeder(AppDbContext db, IPasswordHasher hasher, IStorageProviderFactory storageFactory,
        IDateTime clock, ILogger<DbSeeder> logger)
        => (_db, _hasher, _storageFactory, _clock, _logger) = (db, hasher, storageFactory, clock, logger);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(ct))
        {
            _logger.LogInformation("Seed skipped — users already exist.");
            return;
        }

        _logger.LogInformation("Seeding demo data…");
        var now = _clock.UtcNow;

        // ---- Organization ----
        var org = new Organization
        {
            Name = "XYZ Company",
            Slug = "xyz-company",
            DemoMode = true,
            CreatedAt = now
        };
        _db.Organizations.Add(org);

        // ---- Users (admin + team) ----
        var admin = new User
        {
            OrganizationId = org.Id, Email = AdminEmail, PasswordHash = _hasher.Hash(AdminPassword),
            FullName = "Prasad", Initials = "PG", Role = UserRole.Approver, IsActive = true, CreatedAt = now
        };
        var anita = new User
        {
            OrganizationId = org.Id, Email = ReviewerEmail, PasswordHash = _hasher.Hash(ReviewerPassword),
            FullName = "Anita Rao (Reviewer)", Initials = "AR", Role = UserRole.Uploader, IsActive = true, CreatedAt = now
        };
        var ravi = new User
        {
            OrganizationId = org.Id, Email = "ravi@tenderdocs.io", FullName = "Ravi Menon",
            Initials = "RM", Role = UserRole.Uploader, IsActive = true, CreatedAt = now
        };
        var sara = new User
        {
            OrganizationId = org.Id, Email = ViewerEmail, PasswordHash = _hasher.Hash(ViewerPassword),
            FullName = "Sara Joseph (Viewer)", Initials = "SJ", Role = UserRole.Viewer, IsActive = true, CreatedAt = now
        };
        _db.Users.AddRange(admin, anita, ravi, sara);

        // ---- Folder tree (XYZ Company → …) ----
        var fOrg = Folder(org, null, "XYZ Company");
        var fFin = Folder(org, fOrg, "Finance");
        var fTax = Folder(org, fFin, "Tax");
        var fGst = Folder(org, fTax, "GST");
        var fItr = Folder(org, fTax, "IT Returns");
        var fPan = Folder(org, fTax, "PAN");
        var fBank = Folder(org, fFin, "Bank Statements");
        var fLegal = Folder(org, fOrg, "Legal & Compliance");
        var fMsme = Folder(org, fLegal, "MSME");
        var fTech = Folder(org, fOrg, "Technical");
        var fExp = Folder(org, fTech, "Experience Certificates");
        _db.Folders.AddRange(fOrg, fFin, fTax, fGst, fItr, fPan, fBank, fLegal, fMsme, fTech, fExp);

        // ---- Tags ----
        var tagNames = new[] { "GST Department", "FY 2024-25", "urgent", "fy2025", "GST", "registration",
            "ITR", "income tax", "PAN", "identity", "MSME", "Udyam", "balance sheet", "audit",
            "tender", "NHAI", "bank", "financial", "legal" };
        var tags = tagNames.ToDictionary(n => n, n => new Tag { OrganizationId = org.Id, Name = n, CreatedAt = now });
        _db.Tags.AddRange(tags.Values);

        // ---- Documents (with real stored files) ----
        var provider = await _storageFactory.GetActiveProviderAsync(org.Id, ct);

        async Task<Document> Doc(string name, DocumentType type, string authority, string? fy,
            Folder folder, User uploader, DateOnly? expiry, string[] docTags)
        {
            var (key, size, checksum) = await StoreSample(provider, name, ct);
            var doc = new Document
            {
                OrganizationId = org.Id, FolderId = folder.Id, Name = name, DocumentType = type,
                IssuingAuthority = authority, FinancialYear = fy, ExpiryDate = expiry,
                StorageProvider = provider.ProviderType, StorageKey = key, FileSizeBytes = size,
                ContentType = "application/pdf", Checksum = checksum,
                UploadedById = uploader.Id, CreatedAt = now
            };
            foreach (var tn in docTags)
                if (tags.TryGetValue(tn, out var tag))
                    doc.DocumentTags.Add(new DocumentTag { Tag = tag });
            _db.Documents.Add(doc);
            return doc;
        }

        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var dGst1 = await Doc("sample.pdf", DocumentType.Gst, "GST Department", "FY 2024-25", fGst, admin,
            today.AddMonths(10), new[] { "GST Department", "FY 2024-25", "urgent", "fy2025" });
        var dGst2 = await Doc("GST-Registration-2025.pdf", DocumentType.Gst, "GST Department", "FY 2025-26", fGst, admin,
            today.AddMonths(14), new[] { "GST", "registration" });
        var dItr = await Doc("ITR-Acknowledgement-FY24.pdf", DocumentType.Itr, "Income Tax Dept", "FY 2024-25", fItr, anita,
            today.AddMonths(8), new[] { "ITR", "income tax" });
        var dPan = await Doc("PAN-Card.pdf", DocumentType.Pan, "Income Tax Dept", null, fPan, admin,
            null, new[] { "PAN", "identity" });
        var dMsme = await Doc("MSME-Udyam-Certificate.pdf", DocumentType.Msme, "Ministry of MSME", null, fMsme, anita,
            today.AddMonths(20), new[] { "MSME", "Udyam" });
        var dBal = await Doc("Audited-Financials-FY24.pdf", DocumentType.BalanceSheet, "KPMG", "FY 2024-25", fFin, ravi,
            today.AddMonths(6), new[] { "balance sheet", "audit", "financial" });
        var dTender = await Doc("Tender-Form-NHAI.pdf", DocumentType.TenderForm, "NHAI", "FY 2025-26", fExp, ravi,
            today.AddDays(15), new[] { "tender", "NHAI" });
        var dBank = await Doc("Bank-Statement-Q4.pdf", DocumentType.Other, "HDFC Bank", "FY 2024-25", fBank, admin,
            today.AddMonths(4), new[] { "bank", "financial" });
        var dPoa = await Doc("Power-of-Attorney.pdf", DocumentType.Other, "Notary", null, fLegal, anita,
            today.AddMonths(-2), new[] { "legal" });

        // ---- Projects + assignments ----
        Project Proj(string name, string? desc, params Document[] docs)
        {
            var p = new Project
            {
                OrganizationId = org.Id, Name = name, Description = desc,
                CreatedById = admin.Id, CreatedAt = now
            };
            foreach (var d in docs)
                p.Assignments.Add(new ProjectDocumentAssignment { Document = d, AssignedById = admin.Id, CreatedAt = now });
            _db.Projects.Add(p);
            return p;
        }

        Proj("IPHONE SUPPLY", null, dGst1);
        Proj("test", null, dGst2);
        Proj("NHAI Bridge 2025", "Demo tender bundle", dTender, dItr);
        Proj("ROAD CONSTRUCTION", "Highway resurfacing tender", dItr, dBank);

        // ---- Notifications (for admin) ----
        _db.Notifications.AddRange(
            Notif(org, admin, NotificationType.DocumentExpiring, "Tender form expiring soon",
                "Tender-Form-NHAI.pdf is approaching its validity window.", dTender.Id, "Document"),
            Notif(org, admin, NotificationType.DocumentExpired, "Power of Attorney expired",
                "Power-of-Attorney.pdf has expired. Replace it before submission.", dPoa.Id, "Document"),
            Notif(org, admin, NotificationType.DocumentUploaded, "New document uploaded",
                "Anita uploaded ITR-Acknowledgement-FY24.pdf.", dItr.Id, "Document"));

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seed complete: 1 org, 4 users, {Folders} folders, {Docs} documents, 4 projects.",
            11, 9);
    }

    // ---- helpers ----

    private Folder Folder(Organization org, Folder? parent, string name)
    {
        var folder = new Folder
        {
            OrganizationId = org.Id,
            ParentFolderId = parent?.Id,
            Name = name,
            Depth = parent is null ? 0 : parent.Depth + 1,
            CreatedAt = _clock.UtcNow
        };
        var basePath = parent?.MaterializedPath ?? "/";
        folder.MaterializedPath = $"{basePath}{folder.Id:N}/";
        return folder;
    }

    private Notification Notif(Organization org, User user, NotificationType type, string title,
        string message, Guid? relatedId, string relatedType)
        => new()
        {
            OrganizationId = org.Id, UserId = user.Id, Type = type, Title = title, Message = message,
            RelatedEntityId = relatedId, RelatedEntityType = relatedType, IsRead = false, CreatedAt = _clock.UtcNow
        };

    /// <summary>Writes a tiny valid PDF to storage so downloads/ZIP work, returns its descriptor.</summary>
    private static async Task<(string key, long size, string? checksum)> StoreSample(
        IStorageProvider provider, string fileName, CancellationToken ct)
    {
        var bytes = MinimalPdf($"TenderDocs sample — {fileName}");
        await using var ms = new MemoryStream(bytes);
        var obj = await provider.UploadFileAsync(ms, fileName, "application/pdf", "seed", ct);
        return (obj.Key, obj.SizeBytes, obj.Checksum);
    }

    private static byte[] MinimalPdf(string text)
    {
        // A minimal single-page PDF. Enough to open in a viewer and to package into a ZIP.
        var safe = text.Replace("(", "[").Replace(")", "]");
        var content = $"BT /F1 16 Tf 72 720 Td ({safe}) Tj ET";
        var sb = new StringBuilder();
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {content.Length} >>\nstream\n{content}\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        sb.Append("%PDF-1.4\n");
        var offsets = new List<int>();
        foreach (var (obj, i) in objects.Select((o, i) => (o, i)))
        {
            offsets.Add(Encoding.ASCII.GetByteCount(sb.ToString()));
            sb.Append($"{i + 1} 0 obj\n{obj}\nendobj\n");
        }
        var xrefPos = Encoding.ASCII.GetByteCount(sb.ToString());
        sb.Append($"xref\n0 {objects.Count + 1}\n");
        sb.Append("0000000000 65535 f \n");
        foreach (var off in offsets) sb.Append($"{off:D10} 00000 n \n");
        sb.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xrefPos}\n%%EOF");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
