using TenderDocs.Domain.Authorization;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Auth;

public record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserDto User);

public record UserDto(Guid Id, string Email, string FullName, string Initials, string Role,
    Guid OrganizationId, string OrganizationName, bool DemoMode, IReadOnlyList<string> Permissions)
{
    /// <summary>
    /// Builds the client-facing user payload, resolving the role's permission set so the
    /// frontend gates UI from the same source of truth the API enforces with.
    /// </summary>
    public static UserDto From(User user, string organizationName, bool demoMode) =>
        new(user.Id, user.Email, user.FullName, user.Initials, user.Role.ToString(),
            user.OrganizationId, organizationName, demoMode,
            RolePermissions.For(user.Role).ToArray());
}
