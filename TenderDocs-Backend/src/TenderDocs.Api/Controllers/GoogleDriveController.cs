using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.GoogleDrive;

namespace TenderDocs.Api.Controllers;

[Route("api/google-drive")]
public class GoogleDriveController : ApiControllerBase
{
    public record ConnectRequest(
        string ClientId, string ClientSecret, string RedirectUri, string FolderId,
        string? AccessToken, string? RefreshToken);

    /// <summary>Active storage provider + Google Drive connection status.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<StorageStatusDto>> Status(CancellationToken ct)
        => Ok(await Mediator.Send(new GetStorageStatusQuery(), ct));

    /// <summary>
    /// Returns the Google consent URL to begin connecting Drive. The browser should navigate to it;
    /// Google redirects back to <c>/api/google-drive/callback</c> to finish the connection. Admin only.
    /// </summary>
    [Authorize(Roles = "Approver")]
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(CancellationToken ct)
    {
        var url = await Mediator.Send(new GetGoogleDriveAuthUrlQuery(), ct);
        return Ok(new { url });
    }

    /// <summary>
    /// OAuth redirect target. Google sends the authorization code here; we exchange it for tokens,
    /// store them encrypted, activate the provider, and bounce the browser back to the settings page.
    /// Unauthenticated by necessity (the request originates from Google's redirect).
    /// </summary>
    [AllowAnonymous]
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect("/settings?drive=error");

        try
        {
            await Mediator.Send(new CompleteGoogleDriveConnectionCommand(code, state), ct);
            return Redirect("/settings?drive=connected");
        }
        catch
        {
            return Redirect("/settings?drive=error");
        }
    }

    /// <summary>
    /// Manually connect Google Drive with pre-obtained tokens (advanced / programmatic use).
    /// Most callers should use the <c>authorize</c> + <c>callback</c> flow instead. Admin only.
    /// </summary>
    [Authorize(Roles = "Approver")]
    [HttpPost("connect")]
    public async Task<ActionResult<StorageStatusDto>> Connect(ConnectRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new ConnectGoogleDriveCommand(
            req.ClientId, req.ClientSecret, req.RedirectUri, req.FolderId,
            req.AccessToken, req.RefreshToken), ct));

    [Authorize(Roles = "Approver")]
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect(CancellationToken ct)
    {
        await Mediator.Send(new DisconnectGoogleDriveCommand(), ct);
        return NoContent();
    }
}
