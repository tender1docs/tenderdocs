using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Projects;

/// <summary>Edit a project's name and/or description. Null fields are left unchanged.</summary>
public record UpdateProjectCommand(Guid Id, string? Name, string? Description) : IRequest<ProjectDto>;

public class UpdateProjectValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectValidator()
    {
        RuleFor(x => x.Name).MaximumLength(150).When(x => x.Name is not null);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
    }
}

public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, ProjectDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public UpdateProjectHandler(IAppDbContext db, ICurrentUser current)
        => (_db, _current) = (db, current);

    public async Task<ProjectDto> Handle(UpdateProjectCommand r, CancellationToken ct)
    {
        var p = await _db.Projects
            .Include(x => x.Assignments)
            .FirstOrDefaultAsync(x => x.Id == r.Id && x.OrganizationId == _current.OrganizationId && !x.IsDeleted, ct)
            ?? throw new NotFoundException("Project", r.Id);

        if (r.Name is not null && !string.IsNullOrWhiteSpace(r.Name)) p.Name = r.Name.Trim();
        if (r.Description is not null) p.Description = r.Description.Trim();

        await _db.SaveChangesAsync(ct);
        return new ProjectDto(p.Id, p.Name, p.Description, p.Assignments.Count, p.CreatedAt, p.CreatedById);
    }
}
