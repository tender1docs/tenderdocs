using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;

namespace TenderDocs.Application.Features.Auth;

public record LoginCommand(string Email, string Password, string? Ip) : IRequest<AuthResultDto>;

public class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTime _clock;

    public LoginHandler(IAppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt, IDateTime clock)
        => (_db, _hasher, _jwt, _clock) = (db, hasher, jwt, clock);

    public async Task<AuthResultDto> Handle(LoginCommand r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.Include(u => u.Organization)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, ct);

        if (user is null || user.PasswordHash is null || !_hasher.Verify(r.Password, user.PasswordHash))
            throw new ForbiddenAccessException("Invalid email or password.");
        if (!user.IsActive) throw new ForbiddenAccessException("This account is disabled.");

        user.LastLoginAt = _clock.UtcNow;
        var (token, exp) = _jwt.CreateAccessToken(user);
        var refresh = new RefreshToken
        {
            UserId = user.Id, Token = _jwt.CreateRefreshToken(),
            ExpiresAt = _clock.UtcNow.AddDays(14), CreatedAt = _clock.UtcNow, CreatedByIp = r.Ip
        };
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync(ct);

        return new AuthResultDto(token, exp, refresh.Token,
            new UserDto(user.Id, user.Email, user.FullName, user.Initials, user.Role.ToString(),
                user.OrganizationId, user.Organization.Name, user.Organization.DemoMode));
    }
}
