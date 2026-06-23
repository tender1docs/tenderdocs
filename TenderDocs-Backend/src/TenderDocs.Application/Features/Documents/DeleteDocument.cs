using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Documents;

public record DeleteDocumentCommand(Guid Id) : IRequest;

public class DeleteDocumentHandler : IRequestHandler<DeleteDocumentCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public DeleteDocumentHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task Handle(DeleteDocumentCommand r, CancellationToken ct)
    {
        var d = await _db.Documents
            .FirstOrDefaultAsync(x => x.Id == r.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Document", r.Id);
        d.IsDeleted = true; d.DeletedAt = _clock.UtcNow; // soft delete; storage object reaped by background job
        await _db.SaveChangesAsync(ct);
    }
}
