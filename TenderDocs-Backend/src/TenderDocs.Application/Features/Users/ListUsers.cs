using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Users;

public record TeamMemberDto(
    Guid Id, string FullName, string Email, string Role, string Initials, bool IsActive);

public record ListUsersQuery : IRequest<IReadOnlyList<TeamMemberDto>>;

public class ListUsersHandler : IRequestHandler<ListUsersQuery, IReadOnlyList<TeamMemberDto>>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public ListUsersHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<IReadOnlyList<TeamMemberDto>> Handle(ListUsersQuery q, CancellationToken ct)
        => await _db.Users
            .Where(u => u.OrganizationId == _current.OrganizationId && !u.IsDeleted)
            .OrderBy(u => u.Role).ThenBy(u => u.FullName)
            .Select(u => new TeamMemberDto(
                u.Id, u.FullName, u.Email, u.Role.ToString(), u.Initials, u.IsActive))
            .ToListAsync(ct);
}
