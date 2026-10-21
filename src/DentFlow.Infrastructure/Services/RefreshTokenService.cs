using System.Security.Cryptography;
using System.Text;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure.Services;

internal sealed class RefreshTokenService(
    ApplicationDbContext db,
    IConfiguration configuration) : IRefreshTokenService
{
    private int ExpiryDays =>
        int.TryParse(configuration["Jwt:RefreshTokenExpiryDays"], out var d) ? d : 7;

    public async Task<IssuedRefreshToken> IssueAsync(
        Guid userId, Guid? tenantId, string? ip, string? userAgent, CancellationToken ct = default)
    {
        // A new login starts a new family.
        return await AppendToFamilyAsync(userId, tenantId, Guid.NewGuid(), ip, userAgent, ct);
    }

    public async Task<ErrorOr<RotatedRefreshToken>> RotateAsync(
        string rawToken, string? ip, string? userAgent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return IdentityErrors.InvalidRefreshToken;

        var hash = Hash(rawToken);

        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is null)
            return IdentityErrors.InvalidRefreshToken;

        if (existing.IsRevoked)
        {
            // Revoked *and* already exchanged means someone is replaying a spent token:
            // the legitimate holder has moved on, so this copy leaked. The whole family
            // is suspect, including whatever token is currently live.
            if (existing.ReplacedByTokenHash is not null)
            {
                await RevokeFamilyAsync(existing.UserId, existing.FamilyId, ct);
                return IdentityErrors.RefreshTokenReuse;
            }

            // Revoked without a replacement is an ordinary logout.
            return IdentityErrors.InvalidRefreshToken;
        }

        if (existing.IsExpired)
            return IdentityErrors.InvalidRefreshToken;

        var replacement = await AppendToFamilyAsync(
            existing.UserId, existing.TenantId, existing.FamilyId, ip, userAgent, ct);

        existing.Revoke(Hash(replacement.Token));
        await db.SaveChangesAsync(ct);

        return new RotatedRefreshToken(existing.UserId, replacement.Token, replacement.ExpiresAt);
    }

    public async Task RevokeAsync(string rawToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken)) return;

        var hash = Hash(rawToken);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null) return;

        // Logging out ends the session, so retire the whole chain rather than just the
        // token presented — otherwise an earlier copy could still be rotated.
        await RevokeFamilyAsync(token.UserId, token.FamilyId, ct);
    }

    private async Task<IssuedRefreshToken> AppendToFamilyAsync(
        Guid userId, Guid? tenantId, Guid familyId, string? ip, string? userAgent, CancellationToken ct)
    {
        var raw = GenerateToken();
        var expiresAt = DateTime.UtcNow.AddDays(ExpiryDays);

        db.RefreshTokens.Add(RefreshToken.Create(
            userId, tenantId, Hash(raw), familyId, expiresAt, ip, Truncate(userAgent, 500)));

        await db.SaveChangesAsync(ct);

        return new IssuedRefreshToken(raw, expiresAt);
    }

    private async Task RevokeFamilyAsync(Guid userId, Guid familyId, CancellationToken ct)
    {
        var family = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);

        foreach (var token in family) token.Revoke();

        await db.SaveChangesAsync(ct);
    }

    private static string GenerateToken() =>
        Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    private static string Hash(string rawToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
