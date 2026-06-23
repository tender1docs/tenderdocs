using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Notifications;

namespace TenderDocs.Api.Controllers;

public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false, CancellationToken ct = default)
        => Ok(await Mediator.Send(new ListNotificationsQuery(unreadOnly), ct));

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new MarkNotificationReadCommand(id), ct);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        await Mediator.Send(new MarkAllNotificationsReadCommand(), ct);
        return NoContent();
    }
}
