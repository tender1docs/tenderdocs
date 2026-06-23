using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Common;

namespace TenderDocs.Infrastructure.Persistence;

/// <summary>Stamps CreatedAt/UpdatedAt + CreatedBy/UpdatedBy on save.</summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _current;
    private readonly IDateTime _clock;
    public AuditableEntityInterceptor(ICurrentUser current, IDateTime clock) => (_current, _clock) = (current, clock);

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;
        var now = _clock.UtcNow;
        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAt == default) entry.Entity.CreatedAt = now;
                entry.Entity.CreatedById ??= _current.UserId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedById = _current.UserId;
            }
        }
    }
}
