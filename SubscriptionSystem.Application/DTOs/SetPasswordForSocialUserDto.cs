using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public class SetPasswordForSocialUserDto
    {
        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long")]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; }
    }
}
