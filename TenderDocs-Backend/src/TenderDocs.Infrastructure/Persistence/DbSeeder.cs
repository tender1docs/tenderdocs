using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Authorization;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Infrastructure.Persistence;

/// <summary>
/// Seeds the minimum required to run — and nothing else (no demo data):
///   1. The permission catalog + the role → permission matrix (idempotently upserted from
///      <see cref="Permissions"/> / <see cref="RolePermissions"/>).
///   2. A single Admin user — the real Google account from <c>Seed:AdminEmail</c>
///      (default <c>tender1docs@gmail.com</c>) — created only when the database has no users yet.
/// Everyone else is provisioned by the Admin in Administration → Users. Runs on every startup;
/// safe to run repeatedly.
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _db;
    private readonly IDateTime _clock;
    private readonly IConfiguration _config;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(AppDbContext db, IDateTime clock, IConfiguration config, ILogger<DbSeeder> logger)
        => (_db, _clock, _config, _logger) = (db, clock, config, logger);

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await SeedPermissionCatalogAsync(ct);
        await SeedAdminAsync(ct);
    }

    /// <summary>Upserts the permission definitions and the default role → permission grants.</summary>
    private async Task SeedPermissionCatalogAsync(CancellationToken ct)
    {
        var existingKeys = (await _db.Permissions.Select(p => p.Key).ToListAsync(ct)).ToHashSet();
        foreach (var def in Permissions.All)
            if (!existingKeys.Contains(def.Key))
                _db.Permissions.Add(new Permission { Key = def.Key, Category = def.Category, Description = def.Description });

        var existingPairs = (await _db.RolePermissions
                .Select(rp => new { rp.Role, rp.PermissionKey }).ToListAsync(ct))
            .Select(x => (x.Role, x.PermissionKey)).ToHashSet();
        foreach (UserRole role in Enum.GetValues<UserRole>())
            foreach (var perm in RolePermissions.For(role))
                if (existingPairs.Add((role, perm)))
                    _db.RolePermissions.Add(new RolePermission { Role = role, PermissionKey = perm });

        if (_db.ChangeTracker.HasChanges())
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Permission catalog seeded/updated.");
        }
    }

    /// <summary>Creates the bootstrap Admin (and its organization) only when no users exist yet.</summary>
    private async Task SeedAdminAsync(CancellationToken ct)
    {
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(ct))
        {
            _logger.LogInformation("Admin seed skipped — users already exist.");
            return;
        }

        var adminEmail = (_config["Seed:AdminEmail"] ?? "tender1docs@gmail.com").Trim().ToLowerInvariant();
        var adminName = _config["Seed:AdminName"] ?? "TenderDocs Admin";
        var orgName = _config["Seed:OrganizationName"] ?? "TenderDocs";

        var org = await _db.Organizations.IgnoreQueryFilters().FirstOrDefaultAsync(ct);
        if (org is null)
        {
            org = new Organization
            {
                Name = orgName,
                Slug = Slugify(orgName),
                DemoMode = false,
                CreatedAt = _clock.UtcNow,
            };
            _db.Organizations.Add(org);
        }

        _db.Users.Add(new User
        {
            OrganizationId = org.Id,
            Email = adminEmail,
            PasswordHash = null,             // Google sign-in only — no password
            FullName = adminName,
            Initials = Initials(adminName),
            Role = UserRole.Admin,
            IsActive = true,
            CreatedAt = _clock.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Seeded administrator {Email} for organization {Org}.", adminEmail, org.Name);
    }

    private static string Slugify(string name)
    {
        var slug = new string(name.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        slug = string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..10] : slug;
    }

    private static string Initials(string name)
    {
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "AD";
        if (parts.Length == 1) return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }
}
