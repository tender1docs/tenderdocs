using MediatR;
using Microsoft.EntityFrameworkCore;
using TenderDocs.Application.Common.Exceptions;
using TenderDocs.Application.Common.Interfaces;
using TenderDocs.Domain.Entities;
using TenderDocs.Domain.Enums;
using System.Text.Json;

namespace TenderDocs.Application.Features.GoogleDrive;

/// <summary>
/// Builds the Google consent URL for connecting Drive. The returned URL embeds a signed,
/// short-lived <c>state</c> token (encrypted org id + expiry) so the anonymous OAuth callback
/// can safely attribute the result to the right organization without a session (CSRF protection).
/// </summary>
public record GetGoogleDriveAuthUrlQuery : IRequest<string>;

public class GetGoogleDriveAuthUrlHandler : IRequestHandler<GetGoogleDriveAuthUrlQuery, string>
{
    private readonly ICurrentUser _current;
    private readonly ISecretProtector _protector;
    private readonly IDateTime _clock;
    private readonly IGoogleOAuthService _google;

    public GetGoogleDriveAuthUrlHandler(ICurrentUser current, ISecretProtector protector,
        IDateTime clock, IGoogleOAuthService google)
        => (_current, _protector, _clock, _google) = (current, protector, clock, google);

    public Task<string> Handle(GetGoogleDriveAuthUrlQuery q, CancellationToken ct)
    {
        if (!_google.GetDriveConfig().IsConfigured)
            throw new ConflictException(
                "Google Drive is not configured on the server. Set the GOOGLE_* environment variables.");

        var orgId = _current.OrganizationId!.Value;
        var expiry = _clock.UtcNow.AddMinutes(15).ToUnixTimeSeconds();
        var state = _protector.Encrypt($"{orgId:N}|{expiry}");
        return Task.FromResult(_google.BuildDriveAuthUrl(state));
    }
}

/// <summary>
/// Completes the Drive connection from the OAuth callback: validates the signed state, exchanges
/// the authorization code for access + refresh tokens, and stores them (encrypted) as the org's
/// active storage connection. Runs unauthenticated (the browser arrives from Google), so the org
/// is taken from the validated state rather than the session.
/// </summary>
public record CompleteGoogleDriveConnectionCommand(string Code, string State) : IRequest<Guid>;

public class CompleteGoogleDriveConnectionHandler
    : IRequestHandler<CompleteGoogleDriveConnectionCommand, Guid>
{
    private readonly IAppDbContext _db;
    private readonly ISecretProtector _protector;
    private readonly IDateTime _clock;
    private readonly IGoogleOAuthService _google;

    public CompleteGoogleDriveConnectionHandler(IAppDbContext db, ISecretProtector protector,
        IDateTime clock, IGoogleOAuthService google)
        => (_db, _protector, _clock, _google) = (db, protector, clock, google);

    public async Task<Guid> Handle(CompleteGoogleDriveConnectionCommand r, CancellationToken ct)
    {
        var orgId = ValidateState(r.State);

        var tokens = await _google.ExchangeDriveCodeAsync(r.Code, ct);
        if (string.IsNullOrEmpty(tokens.AccessToken))
            throw new ConflictException("Google did not return an access token.");

        var cfg = _google.GetDriveConfig();

        // Deactivate any prior Google Drive connection for this org.
        var existing = await _db.StorageConnections
            .Where(c => c.OrganizationId == orgId && c.ProviderType == StorageProviderType.GoogleDrive)
            .ToListAsync(ct);
        foreach (var e in existing) e.IsActive = false;

        var payload = JsonSerializer.Serialize(new
        {
            cfg.ClientId,
            cfg.ClientSecret,
            cfg.RedirectUri,
            cfg.FolderId,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
        });

        _db.StorageConnections.Add(new StorageConnection
        {
            OrganizationId = orgId,
            ProviderType = StorageProviderType.GoogleDrive,
            IsActive = true,
            DisplayName = "Google Drive",
            CredentialsEncrypted = _protector.Encrypt(payload),
            CreatedAt = _clock.UtcNow,
        });

        var org = await _db.Organizations.FirstAsync(o => o.Id == orgId, ct);
        org.DemoMode = false;

        await _db.SaveChangesAsync(ct);
        return orgId;
    }

    private Guid ValidateState(string state)
    {
        string decoded;
        try { decoded = _protector.Decrypt(state); }
        catch { throw new ForbiddenAccessException("Invalid OAuth state."); }

        var parts = decoded.Split('|', 2);
        if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out var orgId)
            || !long.TryParse(parts[1], out var expiryUnix))
            throw new ForbiddenAccessException("Malformed OAuth state.");

        if (DateTimeOffset.FromUnixTimeSeconds(expiryUnix) < _clock.UtcNow)
            throw new ForbiddenAccessException("OAuth state expired. Please try connecting again.");

        return orgId;
    }
}
