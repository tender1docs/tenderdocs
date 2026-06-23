using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Folders;
using TenderDocs.Application.Features.GoogleDrive;

namespace TenderDocs.Api.Controllers;

/// <summary>
/// Storage status + folder tree. The active provider is Local in demo mode and switches to
/// Google Drive once connected (see /api/google-drive).
/// </summary>
[Route("api/storage")]
public class StorageController : ApiControllerBase
{
    /// <summary>Which storage provider is active and whether Google Drive is connected.</summary>
    [HttpGet("status")]
    public async Task<ActionResult<StorageStatusDto>> Status(CancellationToken ct)
        => Ok(await Mediator.Send(new GetStorageStatusQuery(), ct));

    /// <summary>Full organization folder tree (unlimited nesting).</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> Tree([FromQuery] Guid? projectId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetFolderTreeQuery(projectId), ct));
}
