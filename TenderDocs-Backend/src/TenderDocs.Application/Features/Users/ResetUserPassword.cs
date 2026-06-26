using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Users;

/// <summary>Admin: set a new password for a user (for password-based login).</summary>
public record ResetUserPasswordCommand(Guid UserId, string NewPassword) : IRequest;

public class ResetUserPasswordValidator : AbstractValidator<ResetUserPasswordCommand>
{
    public ResetUserPasswordValidator() => RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
}

public class ResetUserPasswordHandler : IRequestHandler<ResetUserPasswordCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IPasswordHasher _hasher;
    private readonly IAuditLogger _audit;
    public ResetUserPasswordHandler(IAppDbContext db, ICurrentUser current, IPasswordHasher hasher, IAuditLogger audit)
        => (_db, _current, _hasher, _audit) = (db, current, hasher, audit);

    public async Task Handle(ResetUserPasswordCommand r, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Id == r.UserId && u.OrganizationId == _current.OrganizationId && !u.IsDeleted, ct)
            ?? throw new NotFoundException("User", r.UserId);

        user.PasswordHash = _hasher.Hash(r.NewPassword);
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, "User", user.Id, new { user.Email, change = "password-reset" }, ct: ct);
    }
}
