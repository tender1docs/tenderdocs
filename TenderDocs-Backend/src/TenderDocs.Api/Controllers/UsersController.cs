using Microsoft.AspNetCore.Mvc;
using TenderDocs.Api.Authorization;
using TenderDocs.Application.Features.Users;
using TenderDocs.Domain.Authorization;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Api.Controllers;

/// <summary>Organization users — powers admin user management (Administration → Users).</summary>
public class UsersController : ApiControllerBase
{
    public record CreateUserRequest(
        string Email, string FullName, string Role,
        string? Password, bool IsActive = true, List<Guid>? ProjectIds = null);
    public record UpdateRoleRequest(string Role);
    public record SetActiveRequest(bool IsActive);

    /// <summary>List all users in the organization.</summary>
    [HasPermission(Permissions.Users.Read)]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await Mediator.Send(new ListUsersQuery(), ct));

    /// <summary>Create a user in the current organization.</summary>
    [HasPermission(Permissions.Users.Manage)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest req, CancellationToken ct)
    {
        var role = ParseRole(req.Role);
        var created = await Mediator.Send(
            new CreateUserCommand(req.Email, req.FullName, req.Password, role, req.IsActive, req.ProjectIds), ct);
        return Ok(created);
    }

    /// <summary>Change a user's role.</summary>
    [HasPermission(Permissions.Users.Manage)]
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, UpdateRoleRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateUserRoleCommand(id, ParseRole(req.Role)), ct));

    /// <summary>Activate or deactivate a user.</summary>
    [HasPermission(Permissions.Users.Manage)]
    [HttpPut("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetActiveRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new SetUserActiveCommand(id, req.IsActive), ct));

    public record ResetPasswordRequest(string Password);

    /// <summary>Set a new password for a user.</summary>
    [HasPermission(Permissions.Users.Manage)]
    [HttpPost("{id:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(Guid id, ResetPasswordRequest req, CancellationToken ct)
    {
        await Mediator.Send(new ResetUserPasswordCommand(id, req.Password), ct);
        return NoContent();
    }

    /// <summary>Soft-delete a user.</summary>
    [HasPermission(Permissions.Users.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    private static UserRole ParseRole(string role) =>
        Enum.TryParse<UserRole>(role, ignoreCase: true, out var parsed) ? parsed : UserRole.Viewer;
}
