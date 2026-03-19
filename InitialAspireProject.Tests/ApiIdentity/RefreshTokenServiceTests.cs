using InitialAspireProject.ApiIdentity.Repository;
using InitialAspireProject.ApiIdentity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InitialAspireProject.Tests.ApiIdentity;

public class RefreshTokenServiceTests
{
    private static (RefreshTokenService Service, ApplicationDbContext Context) CreateService(string? expiryDays = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"RefreshTokenTests_{Guid.NewGuid()}")
            .Options;
        var context = new ApplicationDbContext(options);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RefreshToken:ExpiryDays"] = expiryDays ?? "7"
            })
            .Build();

        return (new RefreshTokenService(context, config), context);
    }

    [Fact]
    public async Task GenerateAsync_ReturnsNonEmptyToken()
    {
        var (service, _) = CreateService();
        var token = await service.GenerateAsync("user1", null);
        Assert.False(string.IsNullOrEmpty(token));
    }

    [Fact]
    public async Task GenerateAsync_StoresHashInDatabase()
    {
        var (service, context) = CreateService();
        var token = await service.GenerateAsync("user1", "TestAgent");

        var stored = await context.RefreshTokens.SingleAsync();
        Assert.Equal("user1", stored.UserId);
        Assert.Equal("TestAgent", stored.DeviceInfo);
        Assert.NotEqual(token, stored.TokenHash); // stored is hash, not raw
        Assert.False(stored.IsRevoked);
        Assert.False(stored.IsExpired);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task GenerateAsync_SetsExpiryFromConfig()
    {
        var (service, context) = CreateService("3");
        await service.GenerateAsync("user1", null);

        var stored = await context.RefreshTokens.SingleAsync();
        var expected = DateTime.UtcNow.AddDays(3);
        Assert.InRange(stored.ExpiresAtUtc, expected.AddMinutes(-1), expected.AddMinutes(1));
    }

    [Fact]
    public async Task GenerateAsync_CreatesUniqueFamily()
    {
        var (service, context) = CreateService();
        await service.GenerateAsync("user1", null);
        await service.GenerateAsync("user1", null);

        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.NotEqual(tokens[0].Family, tokens[1].Family);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ValidToken_ReturnsSuccessAndNewToken()
    {
        var (service, context) = CreateService();
        var rawToken = await service.GenerateAsync("user1", null);

        var result = await service.ValidateAndRotateAsync(rawToken, "NewAgent");

        Assert.True(result.Success);
        Assert.Equal("user1", result.UserId);
        Assert.False(string.IsNullOrEmpty(result.NewRefreshToken));
        Assert.NotEqual(rawToken, result.NewRefreshToken);

        // Old token should be revoked, new one active
        var tokens = await context.RefreshTokens.ToListAsync();
        Assert.Equal(2, tokens.Count);
        var old = tokens.First(t => t.TokenHash == RefreshTokenService.HashToken(rawToken));
        var newStored = tokens.First(t => t.TokenHash == RefreshTokenService.HashToken(result.NewRefreshToken!));
        Assert.True(old.IsRevoked);
        Assert.True(newStored.IsActive);
        Assert.Equal(old.Family, newStored.Family);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_NotFound_ReturnsNotFound()
    {
        var (service, _) = CreateService();
        var result = await service.ValidateAndRotateAsync("nonexistent-token", null);

        Assert.False(result.Success);
        Assert.Equal("NotFound", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_ExpiredToken_ReturnsExpired()
    {
        var (service, context) = CreateService();
        var rawToken = await service.GenerateAsync("user1", null);

        // Force the token to be expired
        var stored = await context.RefreshTokens.SingleAsync();
        stored.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);
        await context.SaveChangesAsync();

        var result = await service.ValidateAndRotateAsync(rawToken, null);

        Assert.False(result.Success);
        Assert.Equal("Expired", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateAndRotateAsync_RevokedToken_DetectsReplayAndRevokesFamily()
    {
        var (service, context) = CreateService();
        var rawToken = await service.GenerateAsync("user1", null);

        // Rotate once (valid use)
        var firstRotation = await service.ValidateAndRotateAsync(rawToken, null);
        Assert.True(firstRotation.Success);

        // Try to reuse the original token (replay attack)
        var replayResult = await service.ValidateAndRotateAsync(rawToken, null);

        Assert.False(replayResult.Success);
        Assert.Equal("ReplayDetected", replayResult.ErrorCode);

        // All tokens in the family should be revoked
        var allTokens = await context.RefreshTokens.ToListAsync();
        Assert.All(allTokens, t => Assert.True(t.IsRevoked));
    }

    [Fact]
    public async Task RevokeAsync_RevokesToken()
    {
        var (service, context) = CreateService();
        var rawToken = await service.GenerateAsync("user1", null);

        await service.RevokeAsync(rawToken);

        var stored = await context.RefreshTokens.SingleAsync();
        Assert.True(stored.IsRevoked);
    }

    [Fact]
    public async Task RevokeAsync_NonExistentToken_DoesNotThrow()
    {
        var (service, _) = CreateService();
        await service.RevokeAsync("does-not-exist");
        // No exception
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesAllUserTokens()
    {
        var (service, context) = CreateService();
        await service.GenerateAsync("user1", null);
        await service.GenerateAsync("user1", null);
        await service.GenerateAsync("user2", null);

        await service.RevokeAllForUserAsync("user1");

        var user1Tokens = await context.RefreshTokens.Where(t => t.UserId == "user1").ToListAsync();
        Assert.All(user1Tokens, t => Assert.True(t.IsRevoked));

        var user2Token = await context.RefreshTokens.SingleAsync(t => t.UserId == "user2");
        Assert.False(user2Token.IsRevoked);
    }

    [Fact]
    public async Task RevokeAllForUserAsync_NoTokens_DoesNotThrow()
    {
        var (service, _) = CreateService();
        await service.RevokeAllForUserAsync("no-such-user");
        // No exception
    }

    [Fact]
    public async Task ValidateAndRotateAsync_PreservesFamilyAcrossRotations()
    {
        var (service, context) = CreateService();
        var rawToken = await service.GenerateAsync("user1", null);
        var firstFamily = (await context.RefreshTokens.SingleAsync()).Family;

        var result1 = await service.ValidateAndRotateAsync(rawToken, null);
        var result2 = await service.ValidateAndRotateAsync(result1.NewRefreshToken!, null);

        var allTokens = await context.RefreshTokens.ToListAsync();
        Assert.Equal(3, allTokens.Count);
        Assert.All(allTokens, t => Assert.Equal(firstFamily, t.Family));
    }
}
