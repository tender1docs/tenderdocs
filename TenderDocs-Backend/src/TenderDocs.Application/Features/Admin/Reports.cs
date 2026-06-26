using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Admin;

public record ReportFile(string FileName, string ContentType, byte[] Content);

/// <summary>Admin: generate a CSV report. Types: users, storage, projects, uploads, approvals, audit.</summary>
public record GenerateReportQuery(string Type) : IRequest<ReportFile>;

public class GenerateReportHandler : IRequestHandler<GenerateReportQuery, ReportFile>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GenerateReportHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<ReportFile> Handle(GenerateReportQuery q, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var type = (q.Type ?? "").Trim().ToLowerInvariant();
        var sb = new StringBuilder();

        switch (type)
        {
            case "users":
            case "user-activity":
                Row(sb, "Name", "Email", "Role", "Status", "Last login");
                foreach (var u in await _db.Users.Where(u => u.OrganizationId == orgId && !u.IsDeleted)
                             .OrderBy(u => u.FullName).ToListAsync(ct))
                    Row(sb, u.FullName, u.Email, u.Role.ToString(), u.IsActive ? "Active" : "Disabled",
                        u.LastLoginAt?.ToString("u") ?? "");
                break;

            case "storage":
                var used = await _db.Documents.Where(d => d.OrganizationId == orgId && !d.IsDeleted)
                    .SumAsync(d => (long?)d.FileSizeBytes, ct) ?? 0;
                var docs = await _db.Documents.CountAsync(d => d.OrganizationId == orgId && !d.IsDeleted, ct);
                var projs = await _db.Projects.CountAsync(p => p.OrganizationId == orgId && !p.IsDeleted, ct);
                Row(sb, "Metric", "Value");
                Row(sb, "Used bytes", used.ToString());
                Row(sb, "Used MB", (used / 1024.0 / 1024.0).ToString("F2"));
                Row(sb, "Documents", docs.ToString());
                Row(sb, "Projects", projs.ToString());
                break;

            case "projects":
                Row(sb, "Name", "Description", "Documents", "Created");
                foreach (var p in await _db.Projects.Where(p => p.OrganizationId == orgId && !p.IsDeleted)
                             .OrderByDescending(p => p.CreatedAt).Select(p => new { p.Name, p.Description, Docs = p.Assignments.Count, p.CreatedAt })
                             .ToListAsync(ct))
                    Row(sb, p.Name, p.Description ?? "", p.Docs.ToString(), p.CreatedAt.ToString("u"));
                break;

            case "uploads":
                Row(sb, "Document", "Type", "Uploaded by", "Uploaded at", "Approval");
                foreach (var d in await _db.Documents.Where(d => d.OrganizationId == orgId && !d.IsDeleted)
                             .OrderByDescending(d => d.CreatedAt)
                             .Select(d => new { d.Name, d.DocumentType, Uploader = d.UploadedBy != null ? d.UploadedBy.FullName : "", d.CreatedAt, d.ApprovalStatus })
                             .ToListAsync(ct))
                    Row(sb, d.Name, d.DocumentType.ToString(), d.Uploader, d.CreatedAt.ToString("u"), d.ApprovalStatus.ToString());
                break;

            case "approvals":
                Row(sb, "Document", "Approval", "Approved by", "Decided at", "Reason");
                foreach (var d in await _db.Documents.Where(d => d.OrganizationId == orgId && !d.IsDeleted)
                             .OrderByDescending(d => d.ApprovalAt)
                             .Select(d => new { d.Name, d.ApprovalStatus, Approver = d.ApprovedBy != null ? d.ApprovedBy.FullName : "", d.ApprovalAt, d.RejectionReason })
                             .ToListAsync(ct))
                    Row(sb, d.Name, d.ApprovalStatus.ToString(), d.Approver, d.ApprovalAt?.ToString("u") ?? "", d.RejectionReason ?? "");
                break;

            case "audit":
            case "audit-logs":
                var logs = await _db.AuditLogs.Where(a => a.OrganizationId == orgId)
                    .OrderByDescending(a => a.CreatedAt).Take(5000)
                    .Select(a => new { a.Action, a.EntityType, a.UserId, a.IpAddress, a.DetailsJson, a.CreatedAt })
                    .ToListAsync(ct);
                var ids = logs.Where(x => x.UserId != null).Select(x => x.UserId!.Value).Distinct().ToList();
                var names = await _db.Users.IgnoreQueryFilters().Where(u => ids.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
                Row(sb, "When", "Action", "Entity", "User", "IP", "Details");
                foreach (var a in logs)
                    Row(sb, a.CreatedAt.ToString("u"), a.Action.ToString(), a.EntityType,
                        a.UserId is { } uid && names.TryGetValue(uid, out var n) ? n : "", a.IpAddress ?? "", a.DetailsJson ?? "");
                break;

            default:
                throw new NotFoundException($"Unknown report type '{q.Type}'.");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return new ReportFile($"{type}-report.csv", "text/csv", bytes);
    }

    private static void Row(StringBuilder sb, params string[] cells)
        => sb.AppendLine(string.Join(",", cells.Select(Escape)));

    private static string Escape(string? v)
    {
        v ??= "";
        if (v.Contains('"') || v.Contains(',') || v.Contains('\n') || v.Contains('\r'))
            return $"\"{v.Replace("\"", "\"\"")}\"";
        return v;
    }
}
