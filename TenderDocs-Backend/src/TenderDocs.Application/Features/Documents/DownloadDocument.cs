using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Application.Features.Documents;

public record DownloadDocumentQuery(Guid Id) : IRequest<DocumentFileDto>;
public record DocumentFileDto(Stream Content, string FileName, string ContentType);

public class DownloadDocumentHandler : IRequestHandler<DownloadDocumentQuery, DocumentFileDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IStorageProviderFactory _factory;
    public DownloadDocumentHandler(IAppDbContext db, ICurrentUser current, IStorageProviderFactory factory)
        => (_db, _current, _factory) = (db, current, factory);

    public async Task<DocumentFileDto> Handle(DownloadDocumentQuery q, CancellationToken ct)
    {
        var d = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == q.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Document", q.Id);
        var provider = _factory.GetProvider(d.StorageProvider);
        var stream = await provider.DownloadFileAsync(d.StorageKey, ct);
        return new DocumentFileDto(stream, d.Name, d.ContentType);
    }
}
