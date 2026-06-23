using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;

namespace TenderDocs.Infrastructure.Storage;

/// <summary>Stores files on the local filesystem (or a mounted volume). Default "Demo Mode" backend.</summary>
public class LocalStorageProvider : IStorageProvider
{
    private readonly string _root;
    public StorageProviderType ProviderType => StorageProviderType.Local;

    public LocalStorageProvider(IConfiguration config)
    {
        _root = config["Storage:Local:RootPath"] ?? "/app/storage";
        Directory.CreateDirectory(_root);
    }

    private string FullPath(string key) => Path.Combine(_root, key.Replace('/', Path.DirectorySeparatorChar));

    public async Task<StorageObject> UploadFileAsync(Stream content, string fileName, string contentType,
        string? folderKey = null, CancellationToken ct = default)
    {
        var safe = SanitizeFolder(folderKey);
        var key = string.IsNullOrEmpty(safe)
            ? $"{Guid.NewGuid():N}_{Sanitize(fileName)}"
            : $"{safe}/{Guid.NewGuid():N}_{Sanitize(fileName)}";

        var path = FullPath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var sha = SHA256.Create();
        await using (var fs = File.Create(path))
        await using (var crypto = new CryptoStream(fs, sha, CryptoStreamMode.Write))
        {
            await content.CopyToAsync(crypto, ct);
        }
        var checksum = Convert.ToHexString(sha.Hash!).ToLowerInvariant();
        var size = new FileInfo(path).Length;
        return new StorageObject(key, size, contentType, checksum);
    }

    public Task<Stream> DownloadFileAsync(string key, CancellationToken ct = default)
    {
        var path = FullPath(key);
        if (!File.Exists(path)) throw new FileNotFoundException($"File not found: {key}");
        Stream s = File.OpenRead(path);
        return Task.FromResult(s);
    }

    public Task DeleteFileAsync(string key, CancellationToken ct = default)
    {
        var path = FullPath(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public Task<string> MoveFileAsync(string key, string destinationFolderKey, CancellationToken ct = default)
    {
        var src = FullPath(key);
        var folder = SanitizeFolder(destinationFolderKey);
        var newKey = string.IsNullOrEmpty(folder) ? Path.GetFileName(key) : $"{folder}/{Path.GetFileName(key)}";
        var dest = FullPath(newKey);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Move(src, dest, overwrite: true);
        return Task.FromResult(newKey);
    }

    public Task<string> CreateFolderAsync(string name, string? parentFolderKey = null, CancellationToken ct = default)
    {
        var folder = SanitizeFolder(parentFolderKey);
        var key = string.IsNullOrEmpty(folder) ? Sanitize(name) : $"{folder}/{Sanitize(name)}";
        Directory.CreateDirectory(FullPath(key));
        return Task.FromResult(key);
    }

    public Task<StorageNode> GetFolderTreeAsync(string? rootKey = null, CancellationToken ct = default)
    {
        var rootPath = string.IsNullOrEmpty(rootKey) ? _root : FullPath(rootKey);
        var node = Build(new DirectoryInfo(rootPath), rootKey ?? "");
        return Task.FromResult(node);
    }

    private StorageNode Build(DirectoryInfo dir, string key)
    {
        var node = new StorageNode { Key = key, Name = dir.Name, IsFolder = true };
        foreach (var sub in dir.GetDirectories())
            node.Children.Add(Build(sub, string.IsNullOrEmpty(key) ? sub.Name : $"{key}/{sub.Name}"));
        foreach (var file in dir.GetFiles())
            node.Children.Add(new StorageNode
            {
                Key = string.IsNullOrEmpty(key) ? file.Name : $"{key}/{file.Name}",
                Name = file.Name, IsFolder = false, SizeBytes = file.Length
            });
        return node;
    }

    public async Task GenerateProjectZipAsync(IEnumerable<ZipEntry> entries, Stream output, CancellationToken ct = default)
    {
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var zipEntry = archive.CreateEntry(entry.PathInZip, CompressionLevel.Optimal);
                await using var entryStream = zipEntry.Open();
                await using var src = await entry.OpenRead(ct);
                await src.CopyToAsync(entryStream, ct);
            }
            catch (OperationCanceledException) { throw; }
            catch
            {
                // Skip files that can't be read (e.g. missing on disk) so one bad file
                // doesn't break the entire bundle.
            }
        }
    }

    private static string Sanitize(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s;
    }
    private static string SanitizeFolder(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        return string.Join('/', s.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Sanitize));
    }
}
