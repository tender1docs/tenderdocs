using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Application.Features.Auth;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Users;

/// <summary>
/// Admin: create a user in the current organization. Password is optional — users typically sign in
/// with Google by email; a password is only needed for password-based login. Optional initial project
/// assignments scope which projects the user can access.
/// </summary>
public record CreateUserCommand(
    string Email, string FullName, string? Password, UserRole Role, bool IsActive, List<Guid>? ProjectIds)
    : IRequest<TeamMemberDto>;

public class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        // Optional password; enforce a minimum length only when one is supplied.
        RuleFor(x => x.Password!).MinimumLength(8).When(x => !string.IsNullOrEmpty(x.Password));
    }
}

public class CreateUserHandler : IRequestHandler<CreateUserCommand, TeamMemberDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;

    public CreateUserHandler(IAppDbContext db, ICurrentUser current, IPasswordHasher hasher,
        IDateTime clock, IAuditLogger audit)
        => (_db, _current, _hasher, _clock, _audit) = (db, current, hasher, clock, audit);

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
            PasswordHash = string.IsNullOrEmpty(r.Password) ? null : _hasher.Hash(r.Password),
            FullName = r.FullName.Trim(),
            Initials = RegisterHandler.Initials(r.FullName),
            Role = r.Role,
            IsActive = r.IsActive,
            CreatedAt = _clock.UtcNow,
        };
        _db.Users.Add(user);

        // Optional initial project assignments (filtered to this organization's projects).
        if (r.ProjectIds is { Count: > 0 })
        {
            var validProjectIds = await _db.Projects
                .Where(p => p.OrganizationId == orgId && r.ProjectIds.Contains(p.Id))
                .Select(p => p.Id).ToListAsync(ct);
            foreach (var pid in validProjectIds)
                _db.UserProjects.Add(new UserProject { UserId = user.Id, ProjectId = pid, CreatedAt = _clock.UtcNow });
        }

        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Create, "User", user.Id,
            new { user.Email, role = user.Role.ToString() }, ct: ct);

        return new TeamMemberDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Initials, user.IsActive);
    }
}
