using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Auth;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Api.Controllers;

[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    public record RegisterRequest(string Email, string Password, string FullName, string? OrganizationName);
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record GoogleLoginRequest(string? Code, string? IdToken, string? RedirectUri);

    /// <summary>Create a new workspace + first (Admin) user.</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register(RegisterRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RegisterCommand(req.Email, req.Password, req.FullName, req.OrganizationName), ct));

    /// <summary>Email + password login.</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new LoginCommand(req.Email, req.Password, ClientIp), ct));

    /// <summary>Exchange a refresh token for a new access token (rotates the refresh token).</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RefreshTokenCommand(req.RefreshToken, ClientIp), ct));

    /// <summary>Login or sign up with Google (auth code or id_token).</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResultDto>> Google(GoogleLoginRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new GoogleLoginCommand(req.Code, req.IdToken, req.RedirectUri, ClientIp), ct));

    /// <summary>Current authenticated user profile.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken ct)
        => Ok(await Mediator.Send(new GetCurrentUserQuery(), ct));

    public record UpdateProfileRequest(string FullName);

    /// <summary>Update the signed-in user's profile (display name).</summary>
    [Authorize]
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMe(UpdateProfileRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateProfileCommand(req.FullName), ct));

    public record SelectRoleRequest(string Role);

    /// <summary>
    /// Self-assign an access role (Approver / Uploader / Viewer) and receive a refreshed token.
    /// Demo affordance — only allowed for demo-mode workspaces.
    /// </summary>
    [Authorize]
    [HttpPost("role")]
    public async Task<ActionResult<AuthResultDto>> SelectRole(SelectRoleRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role))
            return BadRequest($"Unknown role '{req.Role}'.");
        return Ok(await Mediator.Send(new SelectRoleCommand(role, ClientIp), ct));
    }
}
