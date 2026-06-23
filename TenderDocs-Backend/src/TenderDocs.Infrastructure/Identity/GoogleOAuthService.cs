using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using TenderDocs.Application.Common.Interfaces;

namespace TenderDocs.Infrastructure.Identity;

/// <summary>
/// Handles "Login with Google" for the auth screen. Two entry points:
///  - ExchangeCodeAsync: server-side authorization-code exchange (redirect flow).
///  - ValidateIdTokenAsync: verifies an id_token produced by Google Identity Services (one-tap / button).
/// App-level Google client credentials are read from Google:ClientId / Google:ClientSecret.
/// </summary>
public class GoogleOAuthService : IGoogleOAuthService
{
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string AuthEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string DriveScope = "https://www.googleapis.com/auth/drive";

    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;

    public GoogleOAuthService(IHttpClientFactory httpFactory, IConfiguration config)
        => (_httpFactory, _config) = (httpFactory, config);

    private (string clientId, string clientSecret) RequireClient()
    {
        var id = _config["Google:ClientId"];
        var secret = _config["Google:ClientSecret"];
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException(
                "Google:ClientId / Google:ClientSecret are not configured.");
        return (id, secret);
    }

    // ---- Google Drive authorization ----

    public GoogleDriveAppConfig GetDriveConfig() => new(
        _config["Google:ClientId"] ?? "",
        _config["Google:ClientSecret"] ?? "",
        _config["Google:RedirectUri"] ?? "",
        _config["Google:DriveFolderId"] ?? "");

    public string BuildDriveAuthUrl(string state)
    {
        var cfg = GetDriveConfig();
        if (!cfg.IsConfigured)
            throw new InvalidOperationException(
                "Google Drive is not configured. Set Google:ClientId, Google:ClientSecret, " +
                "Google:RedirectUri and Google:DriveFolderId.");

        var query = new Dictionary<string, string>
        {
            ["client_id"] = cfg.ClientId,
            ["redirect_uri"] = cfg.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = DriveScope,
            ["access_type"] = "offline",       // request a refresh token
            ["prompt"] = "consent",            // force consent so a refresh token is always returned
            ["include_granted_scopes"] = "true",
            ["state"] = state,
        };
        var qs = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));
        return $"{AuthEndpoint}?{qs}";
    }

    public async Task<GoogleDriveTokenResult> ExchangeDriveCodeAsync(string code, CancellationToken ct = default)
    {
        var cfg = GetDriveConfig();
        if (!cfg.IsConfigured)
            throw new InvalidOperationException("Google Drive is not configured.");

        var http = _httpFactory.CreateClient();
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = cfg.ClientId,
            ["client_secret"] = cfg.ClientSecret,
            ["redirect_uri"] = cfg.RedirectUri,
            ["grant_type"] = "authorization_code",
        });

        using var resp = await http.PostAsync(TokenEndpoint, form, ct);
        var payload = await resp.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty token response from Google.");
        resp.EnsureSuccessStatusCode();

        var expiry = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn ?? 3600);
        return new GoogleDriveTokenResult(payload.AccessToken, payload.RefreshToken, expiry);
    }

    // ---- Login with Google ----

    public async Task<GoogleUserInfo> ExchangeCodeAsync(
        string code, string redirectUri, CancellationToken ct = default)
    {
        var (clientId, clientSecret) = RequireClient();
        var http = _httpFactory.CreateClient();

        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
        });

        using var resp = await http.PostAsync(TokenEndpoint, form, ct);
        var payload = await resp.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty token response from Google.");
        resp.EnsureSuccessStatusCode();

        if (string.IsNullOrEmpty(payload.IdToken))
            throw new InvalidOperationException("Google token response did not include an id_token.");

        var info = await ValidateIdTokenAsync(payload.IdToken, ct);
        var expiry = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn ?? 3600);
        return info with
        {
            AccessToken = payload.AccessToken,
            RefreshToken = payload.RefreshToken,
            TokenExpiry = expiry
        };
    }

    public async Task<GoogleUserInfo> ValidateIdTokenAsync(string idToken, CancellationToken ct = default)
    {
        var (clientId, _) = RequireClient();
        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            throw new UnauthorizedAccessException("Invalid Google id_token.", ex);
        }

        var fullName = payload.Name
            ?? $"{payload.GivenName} {payload.FamilyName}".Trim();
        if (string.IsNullOrWhiteSpace(fullName)) fullName = payload.Email;

        return new GoogleUserInfo(payload.Subject, payload.Email, fullName);
    }

    private sealed class GoogleTokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("id_token")] public string? IdToken { get; set; }
        [JsonPropertyName("expires_in")] public int? ExpiresIn { get; set; }
        [JsonPropertyName("token_type")] public string? TokenType { get; set; }
    }
}
