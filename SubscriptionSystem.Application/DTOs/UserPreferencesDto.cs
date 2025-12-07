namespace SubscriptionSystem.Application.DTOs
{
    public class UserPreferencesDto
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public bool EnableNotifications { get; set; }
        public bool EnableTwoFactor { get; set; }
    }
}

