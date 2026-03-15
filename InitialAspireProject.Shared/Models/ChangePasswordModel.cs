using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models;

public record ChangePasswordModel
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string NewPassword { get; init; } = string.Empty;
}
