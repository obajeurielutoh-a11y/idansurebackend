namespace SubscriptionSystem.Application.DTOs
{
    public class AdminSignUpDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string Privilege { get; set; }
    }
}

