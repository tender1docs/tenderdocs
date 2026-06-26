using Microsoft.AspNetCore.Mvc;
using TenderDocs.Api.Authorization;
using TenderDocs.Application.Common.Models;
using TenderDocs.Application.Features.Documents;
using TenderDocs.Domain.Authorization;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Api.Controllers;

public class DocumentsController : ApiControllerBase
{
    public record UpdateDocumentRequest(
        string? Name,
        DocumentType? DocumentType,
        string? IssuingAuthority,
        string? FinancialYear,
        string? Notes,
        DateOnly? IssueDate,
        DateOnly? ExpiryDate,
        Guid? FolderId,
        List<string>? Tags);

    public class UploadDocumentRequest
    {
        public IFormFile File { get; set; } = default!;
        public DocumentType DocumentType { get; set; }
        public string? IssuingAuthority { get; set; }
        public string? FinancialYear { get; set; }
        public string? Notes { get; set; }
        public DateOnly? IssueDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public Guid? FolderId { get; set; }
        public Guid? ProjectId { get; set; }
        public string? Tags { get; set; }
    }

    /// <summary>Filterable, paged document list (search, type, authority, FY, project, uploader, expiry, tag).</summary>
    [HasPermission(Permissions.Documents.Read)]
    [HttpGet]
    public async Task<ActionResult<PagedList<DocumentDto>>> List(
        [FromQuery] string? search,
        [FromQuery] DocumentType? documentType,
        [FromQuery] string? issuingAuthority,
        [FromQuery] string? financialYear,
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? uploaderId,
        [FromQuery] string? expiryFilter,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
        => Ok(await Mediator.Send(new ListDocumentsQuery(
            search, documentType, issuingAuthority, financialYear, projectId,
            uploaderId, expiryFilter, tag, page, pageSize), ct));

    [HasPermission(Permissions.Documents.Read)]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Get(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new GetDocumentQuery(id), ct));

    /// <summary>Upload a new document (multipart/form-data).</summary>
    [HasPermission(Permissions.Documents.Upload)]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<DocumentDto>> Upload(
        [FromForm] UploadDocumentRequest request,
        CancellationToken ct = default)
    {
        var file = request.File;

        if (file is null || file.Length == 0)
            return BadRequest("A non-empty file is required.");

        await using var stream = file.OpenReadStream();

        var tagList = string.IsNullOrWhiteSpace(request.Tags)
            ? null
            : request.Tags.Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries).ToList();

        var result = await Mediator.Send(
            new UploadDocumentCommand(
                stream,
                file.FileName,
                file.ContentType ?? "application/octet-stream",
                file.Length,
                request.DocumentType,
                request.IssuingAuthority,
                request.FinancialYear,
                request.Notes,
                request.IssueDate,
                request.ExpiryDate,
                request.FolderId,
                request.ProjectId,
                tagList),
            ct);

        return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
    }

    /// <summary>Download the stored file as a stream.</summary>
    [HasPermission(Permissions.Documents.Download)]
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var file = await Mediator.Send(new DownloadDocumentQuery(id), ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    /// <summary>Edit document metadata / replace details.</summary>
    [HasPermission(Permissions.Documents.Edit)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> Update(
        Guid id,
        UpdateDocumentRequest req,
        CancellationToken ct)
        => Ok(await Mediator.Send(new UpdateDocumentCommand(
            id,
            req.Name,
            req.DocumentType,
            req.IssuingAuthority,
            req.FinancialYear,
            req.Notes,
            req.IssueDate,
            req.ExpiryDate,
            req.FolderId,
            req.Tags), ct));

    [HasPermission(Permissions.Documents.Delete)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await Mediator.Send(new DeleteDocumentCommand(id), ct);
        return NoContent();
    }

    public record RejectDocumentRequest(string? Reason);

    /// <summary>Approve a document.</summary>
    [HasPermission(Permissions.Documents.Approve)]
    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<DocumentDto>> Approve(Guid id, CancellationToken ct)
        => Ok(await Mediator.Send(new ApproveDocumentCommand(id), ct));

    /// <summary>Reject a document with an optional reason.</summary>
    [HasPermission(Permissions.Documents.Approve)]
    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<DocumentDto>> Reject(Guid id, RejectDocumentRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RejectDocumentCommand(id, req.Reason), ct));

    public record RequestChangesRequest(string? Comment);

    /// <summary>Request changes on a pending document (notifies the uploader; keeps it pending).</summary>
    [HasPermission(Permissions.Documents.Approve)]
    [HttpPost("{id:guid}/request-changes")]
    public async Task<ActionResult<DocumentDto>> RequestChanges(Guid id, RequestChangesRequest req, CancellationToken ct)
        => Ok(await Mediator.Send(new RequestChangesCommand(id, req.Comment), ct));
}
