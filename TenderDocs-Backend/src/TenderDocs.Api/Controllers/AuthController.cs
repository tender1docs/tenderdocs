using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Auth;

namespace TenderDocs.Api.Controllers;

[AllowAnonymous]
public class AuthController : ApiControllerBase
{
    public record LoginRequest(string Email, string Password);
    public record RefreshRequest(string RefreshToken);
    public record GoogleLoginRequest(string? Code, string? IdToken, string? RedirectUri);

    /// <summary>Email + password login (for accounts an administrator gave a password).</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login(LoginRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new LoginCommand(req.Email, req.Password, ClientIp), ct));

    /// <summary>Exchange a refresh token for a new access token (rotates the refresh token).</summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> Refresh(RefreshRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RefreshTokenCommand(req.RefreshToken, ClientIp), ct));

    /// <summary>Sign in with Google (auth code or id_token). Only provisioned, active users may enter.</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResultDto>> Google(GoogleLoginRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new GoogleLoginCommand(req.Code, req.IdToken, req.RedirectUri, ClientIp), ct));

    /// <summary>Current authenticated user profile (includes the caller's resolved permission set).</summary>
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
}
