using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Admin;

public record ApprovalQueueItemDto(
    Guid DocumentId, string Name, string DocumentType, string? UploadedByName,
    DateTimeOffset UploadedAt, string Projects, string ApprovalStatus);

/// <summary>Admin/Approver: every document awaiting review, with uploader + project context.</summary>
public record ListApprovalQueueQuery : IRequest<IReadOnlyList<ApprovalQueueItemDto>>;

public class ListApprovalQueueHandler : IRequestHandler<ListApprovalQueueQuery, IReadOnlyList<ApprovalQueueItemDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListApprovalQueueHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<ApprovalQueueItemDto>> Handle(ListApprovalQueueQuery q, CancellationToken ct)
    {
        var raw = await _db.Documents
            .Where(d => d.OrganizationId == _current.OrganizationId && !d.IsDeleted
                && d.ApprovalStatus == DocumentApprovalStatus.Pending)
            .OrderBy(d => d.CreatedAt)
            .Select(d => new
            {
                d.Id, d.Name, d.DocumentType, d.CreatedAt, d.ApprovalStatus,
                UploadedByName = d.UploadedBy != null ? d.UploadedBy.FullName : null,
                Projects = d.Assignments.Where(a => !a.IsDeleted).Select(a => a.Project.Name).ToList(),
            })
            .ToListAsync(ct);

        return raw.Select(d => new ApprovalQueueItemDto(
            d.Id, d.Name, d.DocumentType.ToString(), d.UploadedByName, d.CreatedAt,
            string.Join(", ", d.Projects), d.ApprovalStatus.ToString())).ToList();
    }
}
