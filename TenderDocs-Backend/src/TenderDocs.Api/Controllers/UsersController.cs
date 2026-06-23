using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Users;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Api.Controllers;

/// <summary>Organization users — powers the Team page and admin user management.</summary>
public class UsersController : ApiControllerBase
{
    public record CreateUserRequest(string Email, string FullName, string Password, string Role);

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await Mediator.Send(new ListUsersQuery(), ct));

    /// <summary>Create a user in the current organization. Admin only.</summary>
    [Authorize(Roles = "Approver")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest req, CancellationToken ct)
    {
        var role = Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var parsed) ? parsed : UserRole.Viewer;
        var created = await Mediator.Send(new CreateUserCommand(req.Email, req.FullName, req.Password, role), ct);
        return Ok(created);
    }
}
