using Microsoft.AspNetCore.Authorization;

namespace TenderDocs.Api.Authorization;

/// <summary>An authorization requirement carrying the permission key the caller must hold.</summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}
