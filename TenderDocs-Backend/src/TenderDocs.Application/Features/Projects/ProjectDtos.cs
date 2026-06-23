using TenderDocs.Application.Features.Documents;

namespace TenderDocs.Application.Features.Projects;

public record ProjectDto(Guid Id, string Name, string? Description, int DocumentCount,
    DateTimeOffset CreatedAt, Guid? CreatedById);

public record ProjectDetailDto(Guid Id, string Name, string? Description, int DocumentCount,
    DateTimeOffset CreatedAt, IReadOnlyList<DocumentDto> Documents,
    IReadOnlyList<ProjectRequirementCategoryDto> Categories,
    IReadOnlyList<ProjectRequirementDto> Requirements,
    IReadOnlyList<ProjectAssignmentDto> Assignments);

/// <summary>Which requirement (if any) a project document is mapped to. Null = unmapped.</summary>
public record ProjectAssignmentDto(Guid DocumentId, Guid? RequirementId);

/// <summary>A top-level Organize category with its ordered sub-category rows.</summary>
public record ProjectRequirementCategoryDto(Guid Id, string Name, int SortOrder,
    IReadOnlyList<ProjectRequirementDto> Requirements);

public record ProjectRequirementDto(Guid Id, string Name, string? Description, bool IsMandatory,
    int SortOrder, Guid? CategoryId, Guid? FulfilledByDocumentId);
