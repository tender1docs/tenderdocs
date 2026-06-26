using System.Text.Json;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Infrastructure.Services;

/// <summary>Persists audit-trail entries, defaulting org/user to the current request when not supplied.</summary>
public class AuditLogger : IAuditLogger
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;

    public AuditLogger(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task LogAsync(AuditAction action, string entityType, Guid? entityId = null,
        object? details = null, Guid? organizationId = null, Guid? userId = null,
        string? ipAddress = null, CancellationToken ct = default)
    {
        var orgId = organizationId ?? _current.OrganizationId;
        if (orgId is null) return;   // no tenant context — skip defensively

        _db.AuditLogs.Add(new AuditLog
        {
            OrganizationId = orgId.Value,
            UserId = userId ?? _current.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details),
            IpAddress = ipAddress,
            CreatedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
    }
}
