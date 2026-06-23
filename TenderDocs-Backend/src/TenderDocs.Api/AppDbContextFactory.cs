using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TenderDocs.Infrastructure.Persistence;

namespace TenderDocs.Api;

/// <summary>
/// Design-time factory so <c>dotnet ef</c> can construct the <see cref="AppDbContext"/> for
/// add/scaffold operations without booting the full application host (whose scoped services such as
/// <c>ICurrentUser</c> aren't available at design time). No database is contacted by
/// <c>migrations add</c>, so the placeholder connection string here is only used to satisfy the
/// Npgsql provider; the real connection comes from configuration at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=tenderdocs;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;
        return new AppDbContext(options);
    }
}
