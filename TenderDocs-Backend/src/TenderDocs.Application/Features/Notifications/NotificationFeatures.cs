using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Notifications;

public record NotificationDto(Guid Id, string Type, string Title, string Message,
    Guid? RelatedEntityId, string? RelatedEntityType, bool IsRead, DateTimeOffset CreatedAt);

public record ListNotificationsQuery(bool UnreadOnly = false) : IRequest<IReadOnlyList<NotificationDto>>;

public class ListNotificationsHandler : IRequestHandler<ListNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListNotificationsHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<NotificationDto>> Handle(ListNotificationsQuery q, CancellationToken ct)
        => await _db.Notifications
            .Where(n => n.UserId == _current.UserId && (!q.UnreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedAt).Take(100)
            .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Title, n.Message,
                n.RelatedEntityId, n.RelatedEntityType, n.IsRead, n.CreatedAt))
            .ToListAsync(ct);
}

public record MarkNotificationReadCommand(Guid Id) : IRequest;

public class MarkNotificationReadHandler : IRequestHandler<MarkNotificationReadCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public MarkNotificationReadHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task Handle(MarkNotificationReadCommand r, CancellationToken ct)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == r.Id && x.UserId == _current.UserId, ct)
            ?? throw new NotFoundException("Notification", r.Id);
        n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}

public record MarkAllNotificationsReadCommand : IRequest;

public class MarkAllNotificationsReadHandler : IRequestHandler<MarkAllNotificationsReadCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public MarkAllNotificationsReadHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task Handle(MarkAllNotificationsReadCommand r, CancellationToken ct)
    {
        var items = await _db.Notifications.Where(n => n.UserId == _current.UserId && !n.IsRead).ToListAsync(ct);
        foreach (var n in items) n.IsRead = true;
        await _db.SaveChangesAsync(ct);
    }
}
