using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Auth;

public record RegisterCommand(string Email, string Password, string FullName, string? OrganizationName)
    : IRequest<AuthResultDto>;

public class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
    }
}

public class RegisterHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTime _clock;

    public RegisterHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, IDateTime clock)
        => (_db, _hasher, _jwt, _clock) = (db, hasher, jwt, clock);

    public async Task<AuthResultDto> Handle(RegisterCommand r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("An account with this email already exists.");

        var org = new Organization
        {
            Name = string.IsNullOrWhiteSpace(r.OrganizationName) ? $"{r.FullName}'s Workspace" : r.OrganizationName!,
            Slug = Guid.NewGuid().ToString("N")[..10],
            DemoMode = true,
            CreatedAt = _clock.UtcNow
        };
        _db.Organizations.Add(org);

        var user = new User
        {
            OrganizationId = org.Id,
            Email = email,
            PasswordHash = _hasher.Hash(r.Password),
            FullName = r.FullName.Trim(),
            Initials = Initials(r.FullName),
            Role = UserRole.Approver,         // first user of a workspace gets full access
            CreatedAt = _clock.UtcNow
        };
        _db.Users.Add(user);

        var (token, exp) = _jwt.CreateAccessToken(user);
        var refresh = new RefreshToken
        {
            UserId = user.Id, Token = _jwt.CreateRefreshToken(),
            ExpiresAt = _clock.UtcNow.AddDays(14), CreatedAt = _clock.UtcNow
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        return new AuthResultDto(token, exp, refresh.Token,
            new UserDto(user.Id, user.Email, user.FullName, user.Initials, user.Role.ToString(),
                org.Id, org.Name, org.DemoMode));
    }

    internal static string Initials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "U";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
