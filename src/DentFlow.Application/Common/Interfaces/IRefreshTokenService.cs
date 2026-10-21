using ErrorOr;

namespace DentFlow.Application.Common.Interfaces;

/// <summary>A freshly minted refresh token. The raw value is returned once and never stored.</summary>
public sealed record IssuedRefreshToken(string Token, DateTime ExpiresAt);

/// <summary>Result of a successful rotation: the replacement token and whose session it is.</summary>
public sealed record RotatedRefreshToken(Guid UserId, string Token, DateTime ExpiresAt);

/// <summary>
/// Issues, rotates and revokes refresh tokens. Only hashes are persisted, and every
/// rotation stays in one family so that replaying a spent token can be detected and
/// the whole family revoked.
/// </summary>
public interface IRefreshTokenService
{
    Task<IssuedRefreshToken> IssueAsync(
        Guid userId, Guid? tenantId, string? ip, string? userAgent, CancellationToken ct = default);

    Task<ErrorOr<RotatedRefreshToken>> RotateAsync(
        string rawToken, string? ip, string? userAgent, CancellationToken ct = default);

    Task RevokeAsync(string rawToken, CancellationToken ct = default);
}
