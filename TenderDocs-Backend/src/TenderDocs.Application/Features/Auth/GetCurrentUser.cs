using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Auth;

public record GetCurrentUserQuery : IRequest<UserDto>;

public class GetCurrentUserHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public GetCurrentUserHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<UserDto> Handle(GetCurrentUserQuery q, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == _current.UserId, ct)
            ?? throw new NotFoundException("User", _current.UserId ?? Guid.Empty);
        return new UserDto(user.Id, user.Email, user.FullName, user.Initials, user.Role.ToString(),
            user.OrganizationId, user.Organization.Name, user.Organization.DemoMode);
    }
}
