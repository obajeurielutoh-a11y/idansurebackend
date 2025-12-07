namespace SubscriptionSystem.Application.DTOs
{
    public class GoogleSignInDto
    {
        public string IdToken { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string GoogleId { get; set; }
        public string ProfilePicture { get; set; }
    }
}

