using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models
{
    public record ConfirmEmailModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required]
        public required string Token { get; set; }
    }
}
