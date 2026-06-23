using TenderDocs.Domain.Common;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Entities;

/// <summary>
/// Per-organization storage backend config. For Google Drive we store the OAuth client +
/// folder id and tokens, encrypted at rest (CredentialsEncrypted is AES-encrypted JSON).
/// </summary>
public class StorageConnection : AuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = default!;

    public StorageProviderType ProviderType { get; set; }
    public bool IsActive { get; set; }
    public string DisplayName { get; set; } = default!;

    // Encrypted JSON blob: { clientId, clientSecret, redirectUri, folderId, accessToken, refreshToken, tokenExpiry }
    public string? CredentialsEncrypted { get; set; }
}
