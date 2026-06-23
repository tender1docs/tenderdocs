using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Infrastructure.Identity;

/// <summary>Reads the authenticated principal from the current HTTP request's JWT claims.</summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _accessor;

    public CurrentUser(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public Guid? UserId =>
        Guid.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? Principal?.FindFirstValue("sub"), out var id)
            ? id : null;

    public Guid? OrganizationId =>
        Guid.TryParse(Principal?.FindFirstValue("org"), out var id) ? id : null;

    public string? Email => Principal?.FindFirstValue(ClaimTypes.Email)
                            ?? Principal?.FindFirstValue("email");

    public string? Role => Principal?.FindFirstValue(ClaimTypes.Role);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;
}
