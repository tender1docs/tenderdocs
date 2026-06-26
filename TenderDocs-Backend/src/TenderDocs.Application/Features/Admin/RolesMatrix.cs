using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Admin;

public record PermissionDefDto(string Key, string Category, string Description);
public record RoleGrantsDto(string Role, IReadOnlyList<string> Permissions);
public record RolesMatrixDto(IReadOnlyList<PermissionDefDto> Permissions, IReadOnlyList<RoleGrantsDto> Roles);

/// <summary>
/// Admin: the role → permission matrix (read-only view). Sourced from the seeded `permissions` +
/// `role_permissions` tables, so it reflects exactly what the API enforces.
/// </summary>
public record GetRolesMatrixQuery : IRequest<RolesMatrixDto>;

public class GetRolesMatrixHandler : IRequestHandler<GetRolesMatrixQuery, RolesMatrixDto>
{
    private readonly IAppDbContext _db;
    public GetRolesMatrixHandler(IAppDbContext db) => _db = db;

    public async Task<RolesMatrixDto> Handle(GetRolesMatrixQuery q, CancellationToken ct)
    {
        var perms = await _db.Permissions
            .OrderBy(p => p.Category).ThenBy(p => p.Key)
            .Select(p => new PermissionDefDto(p.Key, p.Category, p.Description))
            .ToListAsync(ct);

        var grants = await _db.RolePermissions
            .Select(rp => new { rp.Role, rp.PermissionKey })
            .ToListAsync(ct);

        var roles = grants
            .GroupBy(g => g.Role)
            .OrderBy(g => g.Key)
            .Select(g => new RoleGrantsDto(g.Key.ToString(), g.Select(x => x.PermissionKey).ToList()))
            .ToList();

        return new RolesMatrixDto(perms, roles);
    }
}
