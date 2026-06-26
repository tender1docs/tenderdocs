using Microsoft.AspNetCore.Mvc;
using TenderDocs.Api.Authorization;
using TenderDocs.Application.Features.Folders;
using TenderDocs.Domain.Authorization;

namespace TenderDocs.Api.Controllers;

public class FoldersController : ApiControllerBase
{
    public record CreateFolderRequest(string Name, Guid? ParentFolderId, Guid? ProjectId);
    public record MoveFolderRequest(Guid? NewParentFolderId);

    /// <summary>Full folder tree (optionally scoped to a project). Built from a single ordered query.</summary>
    [HasPermission(Permissions.Organize.Read)]
    [HttpGet("tree")]
    public async Task<IActionResult> Tree([FromQuery] Guid? projectId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetFolderTreeQuery(projectId), ct));

    [HasPermission(Permissions.Organize.Edit)]
    [HttpPost]
    public async Task<ActionResult<FolderNodeDto>> Create(CreateFolderRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new CreateFolderCommand(req.Name, req.ParentFolderId, req.ProjectId), ct));

    [HasPermission(Permissions.Organize.Edit)]
    [HttpPost("{id:guid}/move")]
    public async Task<IActionResult> Move(Guid id, MoveFolderRequest req, CancellationToken ct)
    {
        await Mediator.Send(new MoveFolderCommand(id, req.NewParentFolderId), ct);
        return NoContent();
    }

    [HasPermission(Permissions.Organize.Edit)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteFolderCommand(id), ct);
        return NoContent();
    }
}
