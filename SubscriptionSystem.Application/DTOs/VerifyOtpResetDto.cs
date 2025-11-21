using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public class VerifyOtpResetDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP is required")]
        public string Otp { get; set; }
    }
}

