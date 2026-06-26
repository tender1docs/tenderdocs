using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace TenderDocs.Application.Common.Interfaces;

/// <summary>Abstraction over EF Core DbContext so Application has no EF dependency on concrete context.</summary>
public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Project> Projects { get; }
    DbSet<Folder> Folders { get; }
    DbSet<Document> Documents { get; }
    DbSet<DocumentVersion> DocumentVersions { get; }
    DbSet<ProjectRequirementCategory> ProjectRequirementCategories { get; }
    DbSet<ProjectRequirement> ProjectRequirements { get; }
    DbSet<ProjectDocumentAssignment> ProjectDocumentAssignments { get; }
    DbSet<Tag> Tags { get; }
    DbSet<DocumentTag> DocumentTags { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<StorageConnection> StorageConnections { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserProject> UserProjects { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>Resolves the authenticated user from the JWT for the current request.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? OrganizationId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}

public interface IDateTime { DateTimeOffset UtcNow { get; } }

/// <summary>Hashes/verifies passwords (BCrypt under the hood).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

/// <summary>Issues JWT access tokens + refresh tokens.</summary>
public interface IJwtTokenService
{
    (string token, DateTimeOffset expiresAt) CreateAccessToken(User user);
    string CreateRefreshToken();
}

/// <summary>AES encrypt/decrypt for storage credentials at rest.</summary>
public interface ISecretProtector
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

/// <summary>Validates Google OAuth codes / id tokens, and drives the Google Drive authorization flow.</summary>
public interface IGoogleOAuthService
{
    Task<GoogleUserInfo> ExchangeCodeAsync(string code, string redirectUri, CancellationToken ct = default);
    Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default);

    // --- Google Drive authorization (distinct from "Login with Google") ---
    /// <summary>App-level Google Drive configuration read from Google:* settings.</summary>
    GoogleDriveAppConfig GetDriveConfig();
    /// <summary>Builds the Google consent URL for Drive access (offline, forces a refresh token).</summary>
    string BuildDriveAuthUrl(string state);
    /// <summary>Exchanges an authorization code for Drive access + refresh tokens.</summary>
    Task<GoogleDriveTokenResult> ExchangeDriveCodeAsync(string code, CancellationToken ct = default);
}

/// <summary>App-level Google Drive credentials/config (from environment / appsettings).</summary>
public record GoogleDriveAppConfig(string ClientId, string ClientSecret, string RedirectUri, string FolderId)
{
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RedirectUri) && !string.IsNullOrWhiteSpace(FolderId);
}

/// <summary>Tokens returned by the Drive authorization-code exchange.</summary>
public record GoogleDriveTokenResult(string? AccessToken, string? RefreshToken, DateTimeOffset ExpiresAt);

public record GoogleUserInfo(string GoogleId, string Email, string FullName, string? AccessToken = null,
    string? RefreshToken = null, DateTimeOffset? TokenExpiry = null);

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// Writes an entry to the audit trail (audit_logs). Organization/user default to the current
/// authenticated request when not supplied — pass them explicitly for actions that happen before
/// authentication (e.g. login).
/// </summary>
public interface IAuditLogger
{
    Task LogAsync(AuditAction action, string entityType, Guid? entityId = null,
        object? details = null, Guid? organizationId = null, Guid? userId = null,
        string? ipAddress = null, CancellationToken ct = default);
}
