using Microsoft.AspNetCore.Identity;

namespace InitialAspireProject.ApiIdentity;

public class ApplicationUser : IdentityUser
{
    public required string FullName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}