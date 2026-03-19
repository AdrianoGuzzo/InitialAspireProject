using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models;

public record RevokeTokenRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}
