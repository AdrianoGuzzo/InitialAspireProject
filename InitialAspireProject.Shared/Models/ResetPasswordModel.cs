using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models
{
    public record ResetPasswordModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required]
        public required string Token { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public required string NewPassword { get; set; }

        [Required]
        [Compare(nameof(NewPassword))]
        public required string ConfirmPassword { get; set; }
    }
}
