using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Enums;

namespace TenderDocs.Application.Features.Admin;

public record StorageStatsDto(
    string Provider, bool GoogleDriveConnected, string? FolderId,
    long UsedBytes, int DocumentCount, int ProjectCount, bool Healthy);

/// <summary>Admin: storage provider + usage stats.</summary>
public record GetStorageStatsQuery : IRequest<StorageStatsDto>;

public class GetStorageStatsHandler : IRequestHandler<GetStorageStatsQuery, StorageStatsDto>
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUser _current;
    private readonly ISecretProtector _protector;
    public GetStorageStatsHandler(IAppDbContext db, ICurrentUser current, ISecretProtector protector)
        => (_db, _current, _protector) = (db, current, protector);

    public async Task<StorageStatsDto> Handle(GetStorageStatsQuery q, CancellationToken ct)
    {
        var orgId = _current.OrganizationId;

        var conn = await _db.StorageConnections
            .Where(c => c.OrganizationId == orgId && c.IsActive)
            .OrderByDescending(c => c.CreatedAt).FirstOrDefaultAsync(ct);

        var provider = "Local";
        var connected = false;
        string? folderId = null;
        if (conn is { ProviderType: StorageProviderType.GoogleDrive })
        {
            provider = "GoogleDrive";
            connected = true;
            try
            {
                var json = _protector.Decrypt(conn.CredentialsEncrypted!);
                folderId = JsonDocument.Parse(json).RootElement.GetProperty("FolderId").GetString();
            }
            catch { /* corrupt/empty creds */ }
        }

        var docs = _db.Documents.Where(d => d.OrganizationId == orgId && !d.IsDeleted);
        var usedBytes = await docs.SumAsync(d => (long?)d.FileSizeBytes, ct) ?? 0;
        var docCount = await docs.CountAsync(ct);
        var projectCount = await _db.Projects.CountAsync(p => p.OrganizationId == orgId && !p.IsDeleted, ct);

        // Healthy = local (always) or drive connection present. (A deeper live ping is a later refinement.)
        var healthy = provider == "Local" || connected;

        return new StorageStatsDto(provider, connected, folderId, usedBytes, docCount, projectCount, healthy);
    }
}
