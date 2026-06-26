using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Interfaces;
using TenderDocs.Infrastructure.Identity;
using TenderDocs.Infrastructure.Persistence;
using TenderDocs.Infrastructure.Services;
using TenderDocs.Infrastructure.Storage;

namespace TenderDocs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration config)
    {
        // ---- Persistence -------------------------------------------------
        // Scoped because it depends on ICurrentUser (scoped); a singleton would
        // capture the root-scope user (no HttpContext) and null out audit stamps.
        services.AddScoped<AuditableEntityInterceptor>();

        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---- Identity / cross-cutting services ---------------------------
        services.AddHttpContextAccessor();
        services.AddHttpClient();

        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddSingleton<IDateTime, DateTimeService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ISecretProtector, SecretProtector>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IGoogleOAuthService, GoogleOAuthService>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuditLogger, AuditLogger>();

        // ---- Storage -----------------------------------------------------
        services.AddSingleton<LocalStorageProvider>();
        services.AddScoped<IStorageProviderFactory, StorageProviderFactory>();

        // ---- Document compression (shrinks files before they reach storage) ----
        services.AddSingleton<IDocumentCompressor, DocumentCompressor>();

        // ---- Startup seeder (permission catalog + bootstrap admin) ----
        services.AddScoped<DbSeeder>();

        // ---- AuthN (JWT bearer) ------------------------------------------
        var jwt = config.GetSection("Jwt");
        var secret = jwt["Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(jwt["Issuer"]),
                ValidIssuer = jwt["Issuer"],
                ValidateAudience = !string.IsNullOrWhiteSpace(jwt["Audience"]),
                ValidAudience = jwt["Audience"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(secret))),
                ClockSkew = TimeSpan.FromMinutes(1),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role,
                NameClaimType = System.Security.Claims.ClaimTypes.Name,
            };
        });

        services.AddAuthorization();

        return services;
    }
}
