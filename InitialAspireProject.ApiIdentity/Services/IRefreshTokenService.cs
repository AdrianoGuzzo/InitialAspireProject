namespace InitialAspireProject.ApiIdentity.Services;

public interface IRefreshTokenService
{
    Task<string> GenerateAsync(string userId, string? deviceInfo);
    Task<RefreshTokenResult> ValidateAndRotateAsync(string refreshToken, string? deviceInfo);
    Task RevokeAsync(string refreshToken);
    Task RevokeAllForUserAsync(string userId);
}

public class RefreshTokenResult
{
    public bool Success { get; set; }
    public string? NewRefreshToken { get; set; }
    public string? UserId { get; set; }
    public string? ErrorCode { get; set; }
}
