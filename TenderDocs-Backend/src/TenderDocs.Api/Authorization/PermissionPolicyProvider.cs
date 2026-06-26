using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace TenderDocs.Api.Authorization;

/// <summary>
/// Materializes an authorization policy for any <c>PERM:&lt;permission&gt;</c> policy name produced by
/// <see cref="HasPermissionAttribute"/>, so individual permissions don't have to be registered up front.
/// Non-permission policy names fall through to the default provider.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        var permission = HasPermissionAttribute.ExtractPermission(policyName);
        if (permission is not null)
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
