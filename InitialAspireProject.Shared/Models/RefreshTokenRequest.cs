using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models;

public record RefreshTokenRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}
