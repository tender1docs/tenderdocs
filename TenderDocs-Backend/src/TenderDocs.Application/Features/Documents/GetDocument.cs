using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Documents;

public record GetDocumentQuery(Guid Id) : IRequest<DocumentDto>;

public class GetDocumentHandler : IRequestHandler<GetDocumentQuery, DocumentDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GetDocumentHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<DocumentDto> Handle(GetDocumentQuery q, CancellationToken ct)
    {
        var d = await _db.Documents.Include(x => x.UploadedBy).Include(x => x.ApprovedBy)
            .Include(x => x.DocumentTags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(x => x.Id == q.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Document", q.Id);
        return DocumentMapping.ToDto(d);
    }
}
