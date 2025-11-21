namespace SubscriptionSystem.Application.DTOs
{
    public class AccountDeletionRequestDto
    {
        public string? Reason { get; set; }
        public string Id { get; set; }
        public string UserId { get; set; }
        public DateTime RequestDate { get; set; }
    }
}

