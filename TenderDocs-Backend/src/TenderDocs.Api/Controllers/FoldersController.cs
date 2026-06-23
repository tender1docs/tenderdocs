using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TenderDocs.Application.Features.Folders;

namespace TenderDocs.Api.Controllers;

public class FoldersController : ApiControllerBase
{
    public record CreateFolderRequest(string Name, Guid? ParentFolderId, Guid? ProjectId);
    public record MoveFolderRequest(Guid? NewParentFolderId);

    /// <summary>Full folder tree (optionally scoped to a project). Built from a single ordered query.</summary>
    [HttpGet("tree")]
    public async Task<IActionResult> Tree([FromQuery] Guid? projectId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetFolderTreeQuery(projectId), ct));

    [Authorize(Roles = "Approver")]
    [HttpPost]
    public async Task<ActionResult<FolderNodeDto>> Create(CreateFolderRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new CreateFolderCommand(req.Name, req.ParentFolderId, req.ProjectId), ct));

    [Authorize(Roles = "Approver")]
    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, MoveFolderRequest req, CancellationToken ct)
    {
        await Mediator.Send(new MoveFolderCommand(id, req.NewParentFolderId), ct);
        return NoContent();
    }

    [Authorize(Roles = "Approver")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteFolderCommand(id), ct);
        return NoContent();
    }
}
