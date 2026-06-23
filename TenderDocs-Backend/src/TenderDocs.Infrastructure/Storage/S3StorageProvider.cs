using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Infrastructure.Storage;

/// <summary>
/// Placeholder for a future AWS S3 / MinIO backend. Implements the same IStorageProvider contract.
/// Add AWSSDK.S3 and wire up the client when needed — the rest of the app stays unchanged.
/// </summary>
public class S3StorageProvider : IStorageProvider
{
    public StorageProviderType ProviderType => StorageProviderType.S3;
    private static NotImplementedException NotReady() => new("S3StorageProvider is not yet implemented.");

    public Task<StorageObject> UploadFileAsync(Stream content, string fileName, string contentType, string? folderKey = null, CancellationToken ct = default) => throw NotReady();
    public Task<Stream> DownloadFileAsync(string key, CancellationToken ct = default) => throw NotReady();
    public Task DeleteFileAsync(string key, CancellationToken ct = default) => throw NotReady();
    public Task<string> MoveFileAsync(string key, string destinationFolderKey, CancellationToken ct = default) => throw NotReady();
    public Task<string> CreateFolderAsync(string name, string? parentFolderKey = null, CancellationToken ct = default) => throw NotReady();
    public Task<StorageNode> GetFolderTreeAsync(string? rootKey = null, CancellationToken ct = default) => throw NotReady();
    public Task GenerateProjectZipAsync(IEnumerable<ZipEntry> entries, Stream output, CancellationToken ct = default) => throw NotReady();
}
