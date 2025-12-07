namespace SubscriptionSystem.Application.DTOs
{
    public class SignInResponseDto
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public bool HasActiveSubscription { get; set; }
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public bool RequiresTwoFactor { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        // New fields
        public bool IsNewUser { get; set; }
        public bool NeedsPassword { get; set; }
    }
}


