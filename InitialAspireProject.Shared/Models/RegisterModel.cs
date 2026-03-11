using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.Shared.Models
{
    public record RegisterModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required]
        [MinLength(8)]
        [MaxLength(128)]
        public required string Password { get; set; }

        [Required]
        [MaxLength(100)]
        public required string FullName { get; set; }
    }
}
