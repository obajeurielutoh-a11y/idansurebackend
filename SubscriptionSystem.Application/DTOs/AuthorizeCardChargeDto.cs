namespace SubscriptionSystem.Application.DTOs
{
    public class AuthorizeCardChargeDto
    {
        public string TransRef { get; set; }
        public AuthorizationDetails Authorization { get; set; }
    }
}
