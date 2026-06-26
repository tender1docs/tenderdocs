using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Admin;

public record AuditLogDto(Guid Id, string Action, string EntityType, Guid? EntityId,
    Guid? UserId, string? UserName, string? DetailsJson, string? IpAddress, DateTimeOffset CreatedAt);

public record PagedAuditLogs(IReadOnlyList<AuditLogDto> Items, int Page, int PageSize, int TotalCount);

/// <summary>Admin: filterable, paged audit trail for the organization.</summary>
public record ListAuditLogsQuery(
    Guid? UserId, string? Action, DateTimeOffset? From, DateTimeOffset? To,
    int Page = 1, int PageSize = 50) : IRequest<PagedAuditLogs>;

public class ListAuditLogsHandler : IRequestHandler<ListAuditLogsQuery, PagedAuditLogs>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListAuditLogsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<PagedAuditLogs> Handle(ListAuditLogsQuery q, CancellationToken ct)
    {
        var query = _db.AuditLogs.Where(a => a.OrganizationId == _current.OrganizationId);

        if (q.UserId is { } uid) query = query.Where(a => a.UserId == uid);
        if (!string.IsNullOrWhiteSpace(q.Action) && Enum.TryParse<AuditAction>(q.Action, true, out var act))
            query = query.Where(a => a.Action == act);
        if (q.From is { } from) query = query.Where(a => a.CreatedAt >= from);
        if (q.To is { } to) query = query.Where(a => a.CreatedAt <= to);

        var total = await query.CountAsync(ct);
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, 200);

        var raw = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * size).Take(size)
            .Select(a => new { a.Id, a.Action, a.EntityType, a.EntityId, a.UserId, a.DetailsJson, a.IpAddress, a.CreatedAt })
            .ToListAsync(ct);

        var userIds = raw.Where(x => x.UserId != null).Select(x => x.UserId!.Value).Distinct().ToList();
        var names = await _db.Users.IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var items = raw.Select(x => new AuditLogDto(
            x.Id, x.Action.ToString(), x.EntityType, x.EntityId, x.UserId,
            x.UserId is { } id && names.TryGetValue(id, out var n) ? n : null,
            x.DetailsJson, x.IpAddress, x.CreatedAt)).ToList();

        return new PagedAuditLogs(items, page, size, total);
    }
}
