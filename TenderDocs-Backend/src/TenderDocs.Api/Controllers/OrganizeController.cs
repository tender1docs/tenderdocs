using Microsoft.AspNetCore.Mvc;
using TenderDocs.Api.Authorization;
using TenderDocs.Application.Features.Assignments;
using TenderDocs.Application.Features.Folders;
using TenderDocs.Application.Features.Organize;
using TenderDocs.Application.Features.Projects;
using TenderDocs.Domain.Authorization;

namespace TenderDocs.Api.Controllers;

/// <summary>
/// Organize module: project requirements, document assignment, the folder tree, and ZIP export.
/// These routes mirror the frontend Organize workspace and reuse the Projects/Folders handlers.
/// </summary>
[Route("api/organize")]
public class OrganizeController : ApiControllerBase
{
    public record AssignRequest(Guid ProjectId, Guid DocumentId, Guid? RequirementId);
    public record UnassignRequest(Guid ProjectId, Guid DocumentId);
    public record CreateCategoryRequest(Guid ProjectId, string Name);
    public record RenameRequest(string Name);
    public record CreateRequirementRequest(Guid ProjectId, Guid CategoryId, string Name);

    /// <summary>Project detail with its documents and requirements.</summary>
    [HasPermission(Permissions.Organize.Read)]
    [HttpGet("project/{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> Project(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetProjectQuery(id), ct));

    /// <summary>Ensure the project has the standard requirement rows (idempotent backfill).</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpPost("ensure-requirements/{projectId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> EnsureRequirements(Guid projectId, CancellationToken ct)
        => Ok(await Mediator.Send(new EnsureProjectRequirementsCommand(projectId), ct));

    /// <summary>Attach a document to a project (optionally fulfilling a requirement).</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpPost("assign-document")]
    public async Task<IActionResult> Assign(AssignRequest req, CancellationToken ct)
    {
        await Mediator.Send(new AddDocumentToProjectCommand(req.ProjectId, req.DocumentId, req.RequirementId), ct);
        return NoContent();
    }

    /// <summary>Detach a document from a project.</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpDelete("unassign-document")]
    public async Task<IActionResult> Unassign([FromBody] UnassignRequest req, CancellationToken ct)
    {
        await Mediator.Send(new RemoveDocumentFromProjectCommand(req.ProjectId, req.DocumentId), ct);
        return NoContent();
    }

    // ---- Categories (top-level Organize groups) ----

    /// <summary>Create a new top-level category in a project.</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpPost("category")]
    public async Task<ActionResult<ProjectDetailDto>> CreateCategory(CreateCategoryRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new CreateCategoryCommand(req.ProjectId, req.Name), ct));

    /// <summary>Rename a category (persists; reflected in the export ZIP folder names).</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpPut("category/{projectId:guid}/{categoryId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> RenameCategory(
        Guid projectId, Guid categoryId, RenameRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RenameCategoryCommand(projectId, categoryId, req.Name), ct));

    /// <summary>Delete a category and its rows; any documents on them become unmapped (Others).</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpDelete("category/{projectId:guid}/{categoryId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> DeleteCategory(Guid projectId, Guid categoryId, CancellationToken ct)
        => Ok(await Mediator.Send(new DeleteCategoryCommand(projectId, categoryId), ct));

    // ---- Requirements (sub-category rows) ----

    /// <summary>Add a new sub-category row under a category.</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpPost("requirement")]
    public async Task<ActionResult<ProjectDetailDto>> CreateRequirement(CreateRequirementRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new CreateRequirementCommand(req.ProjectId, req.CategoryId, req.Name), ct));

    /// <summary>Rename a sub-category row (persists; reflected in the export filename tag).</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpPut("requirement/{projectId:guid}/{requirementId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> RenameRequirement(
        Guid projectId, Guid requirementId, RenameRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RenameRequirementCommand(projectId, requirementId, req.Name), ct));

    /// <summary>Delete a sub-category row; any documents on it become unmapped (Others).</summary>
    [HasPermission(Permissions.Organize.Edit)]
    [HttpDelete("requirement/{projectId:guid}/{requirementId:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> DeleteRequirement(Guid projectId, Guid requirementId, CancellationToken ct)
        => Ok(await Mediator.Send(new DeleteRequirementCommand(projectId, requirementId), ct));

    /// <summary>Folder tree scoped to a project (unlimited nesting, frontend-ready).</summary>
    [HasPermission(Permissions.Organize.Read)]
    [HttpGet("tree/{projectId:guid}")]
    public async Task<IActionResult> Tree(Guid projectId, CancellationToken ct)
        => Ok(await Mediator.Send(new GetFolderTreeQuery(projectId), ct));

    /// <summary>Generate the project bundle as a ZIP (GST/PAN/Financial/Technical/Others).</summary>
    [HasPermission(Permissions.Organize.Read)]
    [HttpGet("export/{projectId:guid}")]
    public async Task<IActionResult> Export(Guid projectId, CancellationToken ct)
    {
        var zip = await Mediator.Send(new GenerateProjectZipQuery(projectId), ct);
        return File(zip.Content, "application/zip", zip.FileName);
    }
}
