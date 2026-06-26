using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Admin;

/// <summary>Admin: broadcast a notification to Everyone / a role / an individual user.</summary>
public record BroadcastNotificationCommand(string Target, Guid? UserId, string Title, string Message)
    : IRequest<int>;

public class BroadcastNotificationHandler : IRequestHandler<BroadcastNotificationCommand, int>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;
    public BroadcastNotificationHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IAuditLogger audit)
        => (_db, _current, _clock, _audit) = (db, current, clock, audit);

    public async Task<int> Handle(BroadcastNotificationCommand r, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.Message))
            throw new ValidationException(new Dictionary<string, string[]>
            { ["message"] = new[] { "Title and message are required." } });

        var orgId = _current.OrganizationId!.Value;
        var users = _db.Users.Where(u => u.OrganizationId == orgId && u.IsActive && !u.IsDeleted);

        var target = r.Target.Trim().ToLowerInvariant();
        List<Guid> recipientIds = target switch
        {
            "everyone" => await users.Select(u => u.Id).ToListAsync(ct),
            "admin" => await users.Where(u => u.Role == UserRole.Admin).Select(u => u.Id).ToListAsync(ct),
            "uploader" => await users.Where(u => u.Role == UserRole.Uploader).Select(u => u.Id).ToListAsync(ct),
            "approver" => await users.Where(u => u.Role == UserRole.Approver).Select(u => u.Id).ToListAsync(ct),
            "viewer" => await users.Where(u => u.Role == UserRole.Viewer).Select(u => u.Id).ToListAsync(ct),
            "user" => r.UserId is { } uid && await users.AnyAsync(u => u.Id == uid, ct)
                ? new List<Guid> { uid }
                : throw new NotFoundException("User", r.UserId ?? Guid.Empty),
            _ => throw new ValidationException(new Dictionary<string, string[]>
                { ["target"] = new[] { $"Unknown target '{r.Target}'." } }),
        };

        foreach (var id in recipientIds)
            _db.Notifications.Add(new Notification
            {
                OrganizationId = orgId, UserId = id, Type = NotificationType.System,
                Title = r.Title.Trim(), Message = r.Message.Trim(), IsRead = false, CreatedAt = _clock.UtcNow,
            });
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Create, "Notification", null,
            new { target = r.Target, recipients = recipientIds.Count }, ct: ct);
        return recipientIds.Count;
    }
}

public record AdminNotificationDto(Guid Id, string Title, string Message, Guid UserId,
    string? UserName, bool IsRead, DateTimeOffset CreatedAt);

/// <summary>Admin: recent notifications across the whole organization.</summary>
public record ListAllNotificationsQuery(int Take = 100) : IRequest<IReadOnlyList<AdminNotificationDto>>;

public class ListAllNotificationsHandler : IRequestHandler<ListAllNotificationsQuery, IReadOnlyList<AdminNotificationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListAllNotificationsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<AdminNotificationDto>> Handle(ListAllNotificationsQuery q, CancellationToken ct)
    {
        var raw = await _db.Notifications
            .Where(n => n.OrganizationId == _current.OrganizationId)
            .OrderByDescending(n => n.CreatedAt).Take(Math.Clamp(q.Take, 1, 300))
            .Select(n => new { n.Id, n.Title, n.Message, n.UserId, n.IsRead, n.CreatedAt })
            .ToListAsync(ct);

        var userIds = raw.Select(x => x.UserId).Distinct().ToList();
        var names = await _db.Users.IgnoreQueryFilters()
            .Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return raw.Select(x => new AdminNotificationDto(
            x.Id, x.Title, x.Message, x.UserId,
            names.TryGetValue(x.UserId, out var n) ? n : null, x.IsRead, x.CreatedAt)).ToList();
    }
}
