using TenderDocs.Domain.Enums;

namespace TenderDocs.Domain.Interfaces;

/// <summary>Result of a file upload, provider-agnostic.</summary>
public record StorageObject(string Key, long SizeBytes, string ContentType, string? Checksum = null);

/// <summary>A node in a folder tree returned by a provider.</summary>
public class StorageNode
{
    public string Key { get; set; } = default!;
    public string Name { get; set; } = default!;
    public bool IsFolder { get; set; }
    public long SizeBytes { get; set; }
    public List<StorageNode> Children { get; set; } = new();
}

/// <summary>An entry to be packed into a project ZIP (path inside the archive + a stream factory).</summary>
public record ZipEntry(string PathInZip, Func<CancellationToken, Task<Stream>> OpenRead);

/// <summary>
/// Storage abstraction. Implemented by LocalStorageProvider, GoogleDriveStorageProvider
/// and (future) S3StorageProvider. Selected per-org via StorageConnection.
/// </summary>
public interface IStorageProvider
{
    StorageProviderType ProviderType { get; }

    Task<StorageObject> UploadFileAsync(Stream content, string fileName, string contentType,
        string? folderKey = null, CancellationToken ct = default);

    Task<Stream> DownloadFileAsync(string key, CancellationToken ct = default);

    Task DeleteFileAsync(string key, CancellationToken ct = default);

    Task<string> MoveFileAsync(string key, string destinationFolderKey, CancellationToken ct = default);

    Task<string> CreateFolderAsync(string name, string? parentFolderKey = null, CancellationToken ct = default);

    Task<StorageNode> GetFolderTreeAsync(string? rootKey = null, CancellationToken ct = default);

    /// <summary>Streams a ZIP built from the supplied entries into <paramref name="output"/>.</summary>
    Task GenerateProjectZipAsync(IEnumerable<ZipEntry> entries, Stream output, CancellationToken ct = default);
}

/// <summary>Resolves the active provider for the current organization/context.</summary>
public interface IStorageProviderFactory
{
    Task<IStorageProvider> GetActiveProviderAsync(Guid organizationId, CancellationToken ct = default);
    IStorageProvider GetProvider(StorageProviderType type);
}
