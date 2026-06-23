namespace TenderDocs.Application.Features.Auth;

public record AuthResultDto(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    UserDto User);

public record UserDto(Guid Id, string Email, string FullName, string Initials, string Role,
    Guid OrganizationId, string OrganizationName, bool DemoMode);
