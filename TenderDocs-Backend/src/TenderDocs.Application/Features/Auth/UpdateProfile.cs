using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Application.Features.Auth;

/// <summary>Lets the signed-in user edit their own profile (display name).</summary>
public record UpdateProfileCommand(string FullName) : IRequest<UserDto>;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileValidator() => RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
}

public class UpdateProfileHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    public UpdateProfileHandler(IAppDbContext db, ICurrentUser current) => (_db, _current) = (db, current);

    public async Task<UserDto> Handle(UpdateProfileCommand r, CancellationToken ct)
    {
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Id == _current.UserId, ct)
            ?? throw new NotFoundException("User", _current.UserId ?? Guid.Empty);

        user.FullName = r.FullName.Trim();
        user.Initials = RegisterHandler.Initials(user.FullName);
        await _db.SaveChangesAsync(ct);

        return new UserDto(user.Id, user.Email, user.FullName, user.Initials, user.Role.ToString(),
            user.OrganizationId, user.Organization.Name, user.Organization.DemoMode);
    }
}
