using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public class AdminResetPasswordDto
    {
        [Required]
        public string Email { get; set; } // Or whatever identifier you use

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; }

        [Required]
        [Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}
