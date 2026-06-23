using System.IO.Compression;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using DriveData = Google.Apis.Drive.v3.Data;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;
using TenderDocs.Domain.Interfaces;
using Google.Apis.Auth.OAuth2.Flows;

namespace TenderDocs.Infrastructure.Storage;

/// <summary>
/// Google Drive backed storage. Credentials (clientId/secret/refreshToken/folderId) come from the
/// org's StorageConnection (decrypted). Uses the configured folder id as the root.
/// </summary>
public class GoogleDriveStorageProvider : IStorageProvider
{
    private readonly DriveService _drive;
    private readonly string _rootFolderId;
    public StorageProviderType ProviderType => StorageProviderType.GoogleDrive;

    public GoogleDriveStorageProvider(GoogleDriveCredentials creds)
    {
        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = creds.ClientId, ClientSecret = creds.ClientSecret },
            Scopes = new[] { DriveService.Scope.Drive }
        });
        var token = new Google.Apis.Auth.OAuth2.Responses.TokenResponse
        {
            AccessToken = creds.AccessToken, RefreshToken = creds.RefreshToken
        };
        var credential = new UserCredential(flow, "user", token);
        _drive = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential, ApplicationName = "TenderDocs"
        });
        _rootFolderId = creds.FolderId;
    }

    public async Task<StorageObject> UploadFileAsync(Stream content, string fileName, string contentType,
        string? folderKey = null, CancellationToken ct = default)
    {
        var meta = new DriveData.File
        {
            Name = fileName,
            Parents = new[] { string.IsNullOrEmpty(folderKey) ? _rootFolderId : folderKey }
        };
        var request = _drive.Files.Create(meta, content, contentType);
        request.Fields = "id, size";
        var progress = await request.UploadAsync(ct);
        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
            throw new IOException($"Google Drive upload failed: {progress.Exception?.Message}");
        var file = request.ResponseBody;
        return new StorageObject(file.Id, file.Size ?? 0, contentType);
    }

    public async Task<Stream> DownloadFileAsync(string key, CancellationToken ct = default)
    {
        var ms = new MemoryStream();
        await _drive.Files.Get(key).DownloadAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public Task DeleteFileAsync(string key, CancellationToken ct = default)
        => _drive.Files.Delete(key).ExecuteAsync(ct);

    public async Task<string> MoveFileAsync(string key, string destinationFolderKey, CancellationToken ct = default)
    {
        var get = _drive.Files.Get(key);
        get.Fields = "parents";
        var file = await get.ExecuteAsync(ct);
        var update = _drive.Files.Update(new DriveData.File(), key);
        update.AddParents = string.IsNullOrEmpty(destinationFolderKey) ? _rootFolderId : destinationFolderKey;
        update.RemoveParents = file.Parents is null ? null : string.Join(",", file.Parents);
        update.Fields = "id, parents";
        await update.ExecuteAsync(ct);
        return key;
    }

    public async Task<string> CreateFolderAsync(string name, string? parentFolderKey = null, CancellationToken ct = default)
    {
        var meta = new DriveData.File
        {
            Name = name, MimeType = "application/vnd.google-apps.folder",
            Parents = new[] { string.IsNullOrEmpty(parentFolderKey) ? _rootFolderId : parentFolderKey }
        };
        var request = _drive.Files.Create(meta);
        request.Fields = "id";
        var created = await request.ExecuteAsync(ct);
        return created.Id;
    }

    public async Task<StorageNode> GetFolderTreeAsync(string? rootKey = null, CancellationToken ct = default)
    {
        var root = rootKey ?? _rootFolderId;
        var node = new StorageNode { Key = root, Name = "root", IsFolder = true };
        await PopulateAsync(node, root, ct);
        return node;
    }

    private async Task PopulateAsync(StorageNode parent, string folderId, CancellationToken ct)
    {
        var list = _drive.Files.List();
        list.Q = $"'{folderId}' in parents and trashed = false";
        list.Fields = "files(id, name, mimeType, size)";
        var result = await list.ExecuteAsync(ct);
        foreach (var f in result.Files)
        {
            var isFolder = f.MimeType == "application/vnd.google-apps.folder";
            var child = new StorageNode { Key = f.Id, Name = f.Name, IsFolder = isFolder, SizeBytes = f.Size ?? 0 };
            parent.Children.Add(child);
            if (isFolder) await PopulateAsync(child, f.Id, ct);
        }
    }

    public async Task GenerateProjectZipAsync(IEnumerable<ZipEntry> entries, Stream output, CancellationToken ct = default)
    {
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
        foreach (var entry in entries)
        {
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
}

public record GoogleDriveCredentials(string ClientId, string ClientSecret, string RedirectUri,
    string FolderId, string? AccessToken, string? RefreshToken);
