using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TenderDocs.Domain.Authorization;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Api.Authorization;

/// <summary>
/// Grants a <see cref="PermissionRequirement"/> when the caller's role (from the JWT role claim)
/// maps to that permission via <see cref="RolePermissions"/>. This is the single place role → permission
/// resolution happens for the API surface.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var roleClaim = context.User.FindFirstValue(ClaimTypes.Role);

        if (Enum.TryParse<UserRole>(roleClaim, ignoreCase: true, out var role)
            && RolePermissions.Has(role, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
