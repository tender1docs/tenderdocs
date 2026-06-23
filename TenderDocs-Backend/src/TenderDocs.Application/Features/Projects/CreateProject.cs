using FluentValidation;
using MediatR;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Projects;

public record CreateProjectCommand(string Name, string? Description, IReadOnlyList<string>? Requirements)
    : IRequest<ProjectDto>;

public class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
}

public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public CreateProjectHandler(IAppDbContext db, ICurrentUser current, IDateTime clock)
        => (_db, _current, _clock) = (db, current, clock);

    public async Task<ProjectDto> Handle(CreateProjectCommand r, CancellationToken ct)
    {
        var project = new Project
        {
            OrganizationId = _current.OrganizationId!.Value,
            Name = r.Name.Trim(), Description = r.Description,
            CreatedById = _current.UserId, CreatedAt = _clock.UtcNow
        };
        if (r.Requirements is not null && r.Requirements.Any(n => !string.IsNullOrWhiteSpace(n)))
        {
            // Explicit requirement names: drop them under a single default "Financial" category so
            // they still appear grouped in the (now two-level) Organize workspace.
            var category = new ProjectRequirementCategory { Name = "Financial", SortOrder = 0, CreatedAt = _clock.UtcNow };
            var i = 0;
            foreach (var name in r.Requirements.Where(n => !string.IsNullOrWhiteSpace(n)))
                category.Requirements.Add(new ProjectRequirement
                {
                    Name = name.Trim(), SortOrder = i++, CreatedAt = _clock.UtcNow
                });
            project.RequirementCategories.Add(category);
        }
        else
        {
            // Seed the standard two-level structure so Organize has categories + rows to map into.
            OrganizeDefaults.SeedStructure(_db, project, _clock);
        }
        _db.Projects.Add(project);
        await _db.SaveChangesAsync(ct);
        return new ProjectDto(project.Id, project.Name, project.Description, 0, project.CreatedAt, project.CreatedById);
    }
}
