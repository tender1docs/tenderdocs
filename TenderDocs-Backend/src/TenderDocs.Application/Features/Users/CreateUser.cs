using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Auth;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Users;

/// <summary>Admin-only: create a new user inside the current organization.</summary>
public record CreateUserCommand(string Email, string FullName, string Password, UserRole Role)
    : IRequest<TeamMemberDto>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, TeamMemberDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTime _clock;

    public CreateUserHandler(IAppDbContext db, ICurrentUser current, IPasswordHasher hasher, IDateTime clock)
        => (_db, _current, _hasher, _clock) = (db, current, hasher, clock);

    public async Task<TeamMemberDto> Handle(CreateUserCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId!.Value;
        var email = r.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("An account with this email already exists.");

        var user = new User
        {
            OrganizationId = orgId,
            Email = email,
            PasswordHash = _hasher.Hash(r.Password),
            FullName = r.FullName.Trim(),
            Initials = RegisterHandler.Initials(r.FullName),
            Role = r.Role,
            IsActive = true,
            CreatedAt = _clock.UtcNow
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        return new TeamMemberDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Initials, user.IsActive);
    }
}
