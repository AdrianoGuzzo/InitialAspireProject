using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models
{
    public record ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }
    }
}
