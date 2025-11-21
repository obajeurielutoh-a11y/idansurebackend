namespace SubscriptionSystem.Domain.Entities
{
    public class ApiKey
    {
        public string Id { get; set; }
        public string Key { get; set; }
        public string UserId { get; set; }
        public User User { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
    }
}