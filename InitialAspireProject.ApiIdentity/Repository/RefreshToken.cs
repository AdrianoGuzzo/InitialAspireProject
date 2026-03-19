namespace InitialAspireProject.ApiIdentity.Repository;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string TokenHash { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public required string Family { get; set; }
    public string? DeviceInfo { get; set; }
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsActive => !IsRevoked && !IsExpired;
}
