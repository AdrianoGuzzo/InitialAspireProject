using System.ComponentModel.DataAnnotations;

namespace InitialAspireProject.ApiIdentity
{
    public class ForgotPasswordModel
    {
        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public required string Email { get; set; }
    }
}
