using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Projects;

public record DeleteProjectCommand(Guid Id) : IRequest;

public class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public DeleteProjectHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task Handle(DeleteProjectCommand r, CancellationToken ct)
    {
        var p = await _db.Projects
            .FirstOrDefaultAsync(x => x.Id == r.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Project", r.Id);
        p.IsDeleted = true; p.DeletedAt = _clock.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}
