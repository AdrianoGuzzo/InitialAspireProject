using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.ApiIdentity
{
    public class LoginModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }

        [Required]
        [MaxLength(128)]
        public required string Password { get; set; }
    }
}
