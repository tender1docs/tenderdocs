using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Projects;

/// <summary>
/// Ensures a project has the default Organize structure (categories + rows) so the workspace always
/// has somewhere to map documents. Creates it only when the project has no categories yet, which
/// also backfills projects created before categories existed. Idempotent. See <see cref="OrganizeDefaults"/>.
/// </summary>
public record EnsureProjectRequirementsCommand(Guid ProjectId) : IRequest<ProjectDetailDto>;

public class EnsureProjectRequirementsHandler : IRequestHandler<EnsureProjectRequirementsCommand, ProjectDetailDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    private readonly IMediator _mediator;

    public EnsureProjectRequirementsHandler(IAppDbContext db, ICurrentUser current, IDateTime clock, IMediator mediator)
        => (_db, _current, _clock, _mediator) = (db, current, clock, mediator);

    public async Task<ProjectDetailDto> Handle(EnsureProjectRequirementsCommand r, CancellationToken ct)
    {
        var project = await _db.Projects
            .Include(p => p.RequirementCategories).ThenInclude(c => c.Requirements)
            .Include(p => p.Requirements)
            .Include(p => p.Assignments).ThenInclude(a => a.Document)
            .FirstOrDefaultAsync(p => p.Id == r.ProjectId && p.OrganizationId == _current.OrganizationId && !p.IsDeleted, ct)
            ?? throw new NotFoundException("Project", r.ProjectId);

        if (!project.RequirementCategories.Any(c => !c.IsDeleted))
        {
            OrganizeDefaults.SeedStructure(_db, project, _clock);
            await _db.SaveChangesAsync(ct);             // persist categories + rows (gives them IDs)

            OrganizeDefaults.AutoMapDocumentsByType(project);
            await _db.SaveChangesAsync(ct);             // then map documents onto the now-persisted rows
        }

        return await _mediator.Send(new GetProjectQuery(project.Id), ct);
    }
}
