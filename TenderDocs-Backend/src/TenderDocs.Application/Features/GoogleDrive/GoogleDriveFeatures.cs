using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.GoogleDrive;

public record StorageStatusDto(string ActiveProvider, bool GoogleDriveConnected, string? GoogleDriveFolderId);

// ----- Connect Google Drive: store client id/secret/redirect/folder id (encrypted) -----
public record ConnectGoogleDriveCommand(
    string ClientId, string ClientSecret, string RedirectUri, string FolderId,
    string? AccessToken, string? RefreshToken) : IRequest<StorageStatusDto>;

public class ConnectGoogleDriveHandler : IRequestHandler<ConnectGoogleDriveCommand, StorageStatusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ISecretProtector _protector;
    private readonly IDateTime _clock;
    private readonly IAuditLogger _audit;
    public ConnectGoogleDriveHandler(IAppDbContext db, ICurrentUser current, ISecretProtector protector, IDateTime clock, IAuditLogger audit)
        => (_db, _current, _protector, _clock, _audit) = (db, current, protector, clock, audit);

    public async Task<StorageStatusDto> Handle(ConnectGoogleDriveCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId!.Value;
        // deactivate any existing google connection
        var existing = await _db.StorageConnections
            .Where(c => c.OrganizationId == orgId && c.ProviderType == StorageProviderType.GoogleDrive)
            .ToListAsync(ct);
        foreach (var e in existing) e.IsActive = false;

        var payload = JsonSerializer.Serialize(new
        {
            r.ClientId, r.ClientSecret, r.RedirectUri, r.FolderId, r.AccessToken, r.RefreshToken
        });

        _db.StorageConnections.Add(new StorageConnection
        {
            OrganizationId = orgId, ProviderType = StorageProviderType.GoogleDrive,
            IsActive = true, DisplayName = "Google Drive",
            CredentialsEncrypted = _protector.Encrypt(payload), CreatedAt = _clock.UtcNow
        });
        // leaving demo mode once a real backend is connected
        var org = await _db.Organizations.FirstAsync(o => o.Id == orgId, ct);
        org.DemoMode = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, "Storage", null, new { provider = "GoogleDrive", action = "connected" }, ct: ct);
        return new StorageStatusDto("GoogleDrive", true, r.FolderId);
    }
}

public record GetStorageStatusQuery : IRequest<StorageStatusDto>;

public class GetStorageStatusHandler : IRequestHandler<GetStorageStatusQuery, StorageStatusDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ISecretProtector _protector;
    public GetStorageStatusHandler(IAppDbContext db, ICurrentUser current, ISecretProtector protector)
        => (_db, _current, _protector) = (db, current, protector);

    public async Task<StorageStatusDto> Handle(GetStorageStatusQuery q, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var conn = await _db.StorageConnections
            .Where(c => c.OrganizationId == orgId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync(ct);
        if (conn is null || conn.ProviderType != StorageProviderType.GoogleDrive)
            return new StorageStatusDto("Local", false, null);

        string? folderId = null;
        try
        {
            var json = _protector.Decrypt(conn.CredentialsEncrypted!);
            folderId = JsonDocument.Parse(json).RootElement.GetProperty("FolderId").GetString();
        }
        catch { /* corrupt/empty creds -> treated as not configured */ }
        return new StorageStatusDto("GoogleDrive", true, folderId);
    }
}

public record DisconnectGoogleDriveCommand : IRequest;

public class DisconnectGoogleDriveHandler : IRequestHandler<DisconnectGoogleDriveCommand>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly IAuditLogger _audit;
    public DisconnectGoogleDriveHandler(IAppDbContext db, ICurrentUser current, IAuditLogger audit)
        => (_db, _current, _audit) = (db, current, audit);

    public async Task Handle(DisconnectGoogleDriveCommand r, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;
        var conns = await _db.StorageConnections
            .Where(c => c.OrganizationId == orgId && c.ProviderType == StorageProviderType.GoogleDrive && c.IsActive)
            .ToListAsync(ct);
        foreach (var c in conns) c.IsActive = false;
        await _db.SaveChangesAsync(ct);

        await _audit.LogAsync(AuditAction.Update, "Storage", null, new { provider = "GoogleDrive", action = "disconnected" }, ct: ct);
    }
}
