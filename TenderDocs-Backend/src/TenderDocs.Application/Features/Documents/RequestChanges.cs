using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Documents;

/// <summary>
/// Approver action: request changes on a pending document. The document stays Pending; the uploader
/// is notified with the reviewer's comment so they can replace/fix it.
/// </summary>
public record RequestChangesCommand(Guid Id, string? Comment) : IRequest<DocumentDto>;

public class RequestChangesHandler : IRequestHandler<RequestChangesCommand, DocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;

    public RequestChangesHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IAuditLogger audit)
        => (_db, _current, _clock, _audit) = (db, current, clock, audit);

    public async Task<DocumentDto> Handle(RequestChangesCommand r, CancellationToken ct)
    {
        var d = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == r.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Document", r.Id);

        var comment = string.IsNullOrWhiteSpace(r.Comment) ? "Changes requested by the reviewer." : r.Comment.Trim();

        if (d.UploadedById is { } uploaderId)
        {
            _db.Notifications.Add(new Notification
            {
                OrganizationId = d.OrganizationId,
                UserId = uploaderId,
                Type = NotificationType.System,
                Title = $"Changes requested: {d.Name}",
                Message = comment,
                RelatedEntityId = d.Id,
                RelatedEntityType = "Document",
                IsRead = false,
                CreatedAt = _clock.UtcNow,
            });
        }
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, "Document", d.Id,
            new { d.Name, change = "request-changes", comment }, ct: ct);

        var saved = await _db.Documents.Include(x => x.UploadedBy).Include(x => x.ApprovedBy)
            .Include(x => x.DocumentTags).ThenInclude(t => t.Tag).FirstAsync(x => x.Id == d.Id, ct);
        return DocumentMapping.ToDto(saved);
    }
}
