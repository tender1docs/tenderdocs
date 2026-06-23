using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;
using TenderDocs.Infrastructure.Persistence;

namespace TenderDocs.Infrastructure.Storage;

public class StorageProviderFactory : IStorageProviderFactory
{
    private readonly AppDbContext _db;
    private readonly ISecretProtector _protector;
    private readonly IConfiguration _config;
    private readonly LocalStorageProvider _local;

    public StorageProviderFactory(AppDbContext db, ISecretProtector protector,
        IConfiguration config, LocalStorageProvider local)
        => (_db, _protector, _config, _local) = (db, protector, config, local);

    public async Task<IStorageProvider> GetActiveProviderAsync(Guid organizationId, CancellationToken ct = default)
    {
        var conn = await _db.StorageConnections
            .Where(c => c.OrganizationId == organizationId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (conn is null) return _local;

        return conn.ProviderType switch
        {
            StorageProviderType.GoogleDrive => BuildGoogleDrive(conn.CredentialsEncrypted!),
            StorageProviderType.S3 => new S3StorageProvider(),
            _ => _local
        };
    }

    public IStorageProvider GetProvider(StorageProviderType type) => type switch
    {
        StorageProviderType.GoogleDrive => ResolveGoogleDriveForCurrentRequest(),
        StorageProviderType.S3 => new S3StorageProvider(),
        _ => _local
    };

    private IStorageProvider ResolveGoogleDriveForCurrentRequest()
    {
        // For downloads we still need the credentials. Callers that know the org should prefer
        // GetActiveProviderAsync; here we fall back to the single active GD connection if present.
        var conn = _db.StorageConnections
            .Where(c => c.ProviderType == StorageProviderType.GoogleDrive && c.IsActive)
            .OrderByDescending(c => c.CreatedAt).FirstOrDefault();
        return conn is null ? _local : BuildGoogleDrive(conn.CredentialsEncrypted!);
    }

    private GoogleDriveStorageProvider BuildGoogleDrive(string encrypted)
    {
        var json = _protector.Decrypt(encrypted);
        var creds = JsonSerializer.Deserialize<GoogleDriveCredentials>(json)!;
        return new GoogleDriveStorageProvider(creds);
    }
}
