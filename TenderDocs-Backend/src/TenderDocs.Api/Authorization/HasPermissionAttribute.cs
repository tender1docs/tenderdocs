using Microsoft.AspNetCore.Authorization;

namespace TenderDocs.Api.Authorization;

/// <summary>
/// Guards an endpoint with a fine-grained permission (e.g. <c>[HasPermission(Permissions.Documents.Upload)]</c>).
/// Encodes the permission into the authorization policy name; <see cref="PermissionPolicyProvider"/>
/// materializes a matching policy on demand so permissions never need pre-registration.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PERM:";

    public HasPermissionAttribute(string permission) => Policy = $"{PolicyPrefix}{permission}";

    /// <summary>Returns the permission encoded in a policy name, or null if it isn't a permission policy.</summary>
    public static string? ExtractPermission(string policyName) =>
        policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal)
            ? policyName[PolicyPrefix.Length..]
            : null;
}
