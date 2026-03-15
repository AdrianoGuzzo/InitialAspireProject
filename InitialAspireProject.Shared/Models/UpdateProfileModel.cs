using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models;

public record UpdateProfileModel
{
    [Required]
    [MaxLength(200)]
    public string FullName { get; init; } = string.Empty;
}
