using Microsoft.AspNetCore.Mvc;
using TenderDocs.Api.Authorization;
using TenderDocs.Application.Features.Assignments;
using TenderDocs.Application.Features.Projects;
using TenderDocs.Domain.Authorization;

namespace TenderDocs.Api.Controllers;

public class ProjectsController : ApiControllerBase
{
    public record CreateProjectRequest(string Name, string? Description, List<string>? Requirements);
    public record UpdateProjectRequest(string? Name, string? Description);
    public record AssignDocumentRequest(Guid DocumentId, Guid? RequirementId);
    public record SetDocumentsRequest(List<Guid> DocumentIds);

    [HasPermission(Permissions.Projects.Read)]
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
        => Ok(await Mediator.Send(new ListProjectsQuery(), ct));

    /// <summary>Project list including the IDs of each project's assigned documents.</summary>
    [HasPermission(Permissions.Projects.Read)]
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(CancellationToken ct)
        => Ok(await Mediator.Send(new ListProjectSummariesQuery(), ct));

    [HasPermission(Permissions.Projects.Read)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailDto>> Get(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetProjectQuery(id), ct));

    [HasPermission(Permissions.Projects.Manage)]
    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create(CreateProjectRequest req, CancellationToken ct)
    {
        var result = await Mediator.Send(new CreateProjectCommand(req.Name, req.Description, req.Requirements), ct);
        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>Edit a project's name and/or description.</summary>
    [HasPermission(Permissions.Projects.Manage)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, UpdateProjectRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateProjectCommand(id, req.Name, req.Description), ct));

    [HasPermission(Permissions.Projects.Manage)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteProjectCommand(id), ct);
        return NoContent();
    }

    // ---- Assignments (documents in a project) ----

    /// <summary>Attach an existing document to the project (optionally fulfilling a requirement).</summary>
    [HasPermission(Permissions.Projects.Assign)]
    [HttpPost("{id:guid}/documents")]
    public async Task<IActionResult> AddDocument(Guid id, AssignDocumentRequest req, CancellationToken ct)
    {
        await Mediator.Send(new AddDocumentToProjectCommand(id, req.DocumentId, req.RequirementId), ct);
        return NoContent();
    }

    [HasPermission(Permissions.Projects.Assign)]
    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> RemoveDocument(Guid id, Guid documentId, CancellationToken ct)
    {
        await Mediator.Send(new RemoveDocumentFromProjectCommand(id, documentId), ct);
        return NoContent();
    }

    /// <summary>Replace the whole set of documents assigned to a project (Organize "set documents").</summary>
    [HasPermission(Permissions.Projects.Assign)]
    [HttpPut("{id:guid}/documents")]
    public async Task<ActionResult<ProjectDetailDto>> SetDocuments(Guid id, SetDocumentsRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new SetProjectDocumentsCommand(id, req.DocumentIds ?? new()), ct));

    /// <summary>Generate and stream the project bundle as a ZIP (grouped GST/PAN/Financial/Technical/Others).</summary>
    [HasPermission(Permissions.Projects.Read)]
    [HttpGet("{id:guid}/zip")]
    public async Task<IActionResult> DownloadZip(Guid id, CancellationToken ct)
    {
        var zip = await Mediator.Send(new GenerateProjectZipQuery(id), ct);
        return File(zip.Content, "application/zip", zip.FileName);
    }
}
