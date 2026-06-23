using TenderDocs.Domain.Common;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = default!;
    public Guid? EntityId { get; set; }
    public string? DetailsJson { get; set; }   // stored as jsonb
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
