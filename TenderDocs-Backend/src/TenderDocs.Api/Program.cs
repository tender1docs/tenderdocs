using System.Reflection;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using TenderDocs.Api.Authorization;
using TenderDocs.Api.Middleware;
using TenderDocs.Application;
using TenderDocs.Infrastructure;
using TenderDocs.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (Serilog) ----
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---- Application + Infrastructure ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Permission-based authorization ----
// A dynamic policy provider materializes a policy for every [HasPermission("area.action")]
// guard, and the handler resolves the caller's role → permissions via Domain RolePermissions.
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// ---- MVC ----
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();

// ---- CORS (frontend) ----
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                  ?? new[] { "http://localhost:5173", "http://localhost:3000" };
builder.Services.AddCors(options => options.AddPolicy("frontend", policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()));

// ---- Swagger (with JWT bearer) ----
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TenderDocs API", Version = "v1" });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token (without the 'Bearer ' prefix).",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = Array.Empty<string>() });

    var xml = Path.Combine(AppContext.BaseDirectory,
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) c.IncludeXmlComments(xml);
});

// ---- Health checks ----
var connectionString = builder.Configuration.GetConnectionString("Default")!;
builder.Services.AddHealthChecks().AddNpgSql(connectionString, name: "postgres");

// ---- Rate limiting (per client IP; stricter on auth to blunt brute-force) ----
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var isAuth = ctx.Request.Path.StartsWithSegments("/api/auth");
        return RateLimitPartition.GetFixedWindowLimiter(
            (isAuth ? "auth:" : "gen:") + ip,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = isAuth ? 20 : 300,   // auth: 20/min, everything else: 300/min
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            });
    });
});

// Trust the reverse proxy (Caddy/nginx) so rate limiting + audit logs see the real client IP,
// not the proxy's. Single-server Docker: trust the front proxy on the compose network.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

// ---- Apply migrations on startup ----
await ApplyMigrationsAsync(app);

// ---- Seed permission catalog + bootstrap admin (idempotent), controlled by Seed:Enabled ----
if (app.Configuration.GetValue("Seed:Enabled", true))
    await SeedDatabaseAsync(app);

// ---- Pipeline ----
app.UseForwardedHeaders();   // real client IP (behind the proxy) — first, so everything downstream sees it
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment() ||
    builder.Configuration.GetValue("Swagger:Enabled", true))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TenderDocs API v1"));
}

app.UseCors("frontend");
app.UseRateLimiter();   // after CORS so preflight isn't throttled; before auth so abuse is rejected early
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

static async Task ApplyMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count > 0)
        {
            logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
            await db.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("Database schema is up to date.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database migration failed on startup.");
        throw;
    }
}

static async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        // Seeding is best-effort: a failure here must not stop the API from serving.
        logger.LogError(ex, "Database seeding failed on startup.");
    }
}

// Exposed for integration testing.
public partial class Program { }
