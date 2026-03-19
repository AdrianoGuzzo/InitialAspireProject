using System.Security.Cryptography;
using InitialAspireProject.ApiIdentity.Repository;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.ApiIdentity.Services;

public class RefreshTokenService : IRefreshTokenService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;

    public RefreshTokenService(ApplicationDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    private const int MaxDeviceInfoLength = 512;

    public async Task<string> GenerateAsync(string userId, string? deviceInfo)
    {
        var rawToken = GenerateRawToken();
        var tokenHash = HashToken(rawToken);
        var expiryDays = int.Parse(_configuration["RefreshToken:ExpiryDays"] ?? "7");
        var truncatedDeviceInfo = TruncateDeviceInfo(deviceInfo);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = tokenHash,
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expiryDays),
            Family = Guid.NewGuid().ToString(),
            DeviceInfo = truncatedDeviceInfo
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();

        return rawToken;
    }

    public async Task<RefreshTokenResult> ValidateAndRotateAsync(string refreshToken, string? deviceInfo)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored is null)
            return new RefreshTokenResult { ErrorCode = "NotFound" };

        if (stored.IsExpired)
            return new RefreshTokenResult { ErrorCode = "Expired" };

        if (stored.IsRevoked)
        {
            await RevokeFamilyAsync(stored.Family);
            return new RefreshTokenResult { ErrorCode = "ReplayDetected" };
        }

        stored.RevokedAtUtc = DateTime.UtcNow;

        var newRawToken = GenerateRawToken();
        var newTokenHash = HashToken(newRawToken);
        stored.ReplacedByTokenHash = newTokenHash;

        var expiryDays = int.Parse(_configuration["RefreshToken:ExpiryDays"] ?? "7");
        var newToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = newTokenHash,
            UserId = stored.UserId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expiryDays),
            Family = stored.Family,
            DeviceInfo = TruncateDeviceInfo(deviceInfo)
        };

        _context.RefreshTokens.Add(newToken);
        await _context.SaveChangesAsync();

        return new RefreshTokenResult
        {
            Success = true,
            NewRefreshToken = newRawToken,
            UserId = stored.UserId
        };
    }

    public async Task RevokeAsync(string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var stored = await _context.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        if (stored is not null && !stored.IsRevoked)
        {
            stored.RevokedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task RevokeAllForUserAsync(string userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in activeTokens)
            token.RevokedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private async Task RevokeFamilyAsync(string family)
    {
        var familyTokens = await _context.RefreshTokens
            .Where(t => t.Family == family && t.RevokedAtUtc == null)
            .ToListAsync();

        foreach (var token in familyTokens)
            token.RevokedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    private static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    internal static string HashToken(string token)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }

    private static string? TruncateDeviceInfo(string? deviceInfo)
        => deviceInfo?.Length > MaxDeviceInfoLength ? deviceInfo[..MaxDeviceInfoLength] : deviceInfo;
}
