using Microsoft.AspNetCore.Mvc;
using TenderDocs.Api.Authorization;
using TenderDocs.Application.Features.Admin;
using TenderDocs.Domain.Authorization;

namespace TenderDocs.Api.Controllers;

/// <summary>Administration portal endpoints: audit, storage, roles, approvals, access, notifications, reports.</summary>
[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    // ---- Audit logs ----
    [HasPermission(Permissions.Audit.View)]
    [HttpGet("audit")]
    public async Task<IActionResult> Audit(
        [FromQuery] Guid? userId, [FromQuery] string? action,
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(await Mediator.Send(new ListAuditLogsQuery(userId, action, from, to, page, pageSize), ct));

    // ---- Storage stats ----
    [HasPermission(Permissions.Storage.Read)]
    [HttpGet("storage")]
    public async Task<IActionResult> Storage(CancellationToken ct)
        => Ok(await Mediator.Send(new GetStorageStatsQuery(), ct));

    // ---- Roles → permission matrix (read-only) ----
    [HasPermission(Permissions.Roles.Manage)]
    [HttpGet("roles")]
    public async Task<IActionResult> Roles(CancellationToken ct)
        => Ok(await Mediator.Send(new GetRolesMatrixQuery(), ct));

    // ---- Approval queue ----
    [HasPermission(Permissions.Documents.Approve)]
    [HttpGet("approvals")]
    public async Task<IActionResult> Approvals(CancellationToken ct)
        => Ok(await Mediator.Send(new ListApprovalQueueQuery(), ct));

    // ---- Project access (assign users to projects) ----
    public record SetProjectsRequest(List<Guid> ProjectIds);

    [HasPermission(Permissions.ProjectAccess.Manage)]
    [HttpGet("users/{userId:guid}/projects")]
    public async Task<IActionResult> UserProjects(Guid userId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetUserProjectsQuery(userId), ct));

    [HasPermission(Permissions.ProjectAccess.Manage)]
    [HttpPut("users/{userId:guid}/projects")]
    public async Task<IActionResult> SetUserProjects(Guid userId, SetProjectsRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new SetUserProjectsCommand(userId, req.ProjectIds ?? new()), ct));

    // ---- Notifications (broadcast + list) ----
    public record BroadcastRequest(string Target, Guid? UserId, string Title, string Message);

    [HasPermission(Permissions.Notifications.Manage)]
    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(CancellationToken ct)
        => Ok(await Mediator.Send(new ListAllNotificationsQuery(), ct));

    [HasPermission(Permissions.Notifications.Manage)]
    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> Broadcast(BroadcastRequest req, CancellationToken ct)
        => Ok(new { recipients = await Mediator.Send(
            new BroadcastNotificationCommand(req.Target, req.UserId, req.Title, req.Message), ct) });

    // ---- Reports (CSV) ----
    [HasPermission(Permissions.Reports.View)]
    [HttpGet("reports/{type}")]
    public async Task<IActionResult> Report(string type, CancellationToken ct)
    {
        var file = await Mediator.Send(new GenerateReportQuery(type), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }
}
