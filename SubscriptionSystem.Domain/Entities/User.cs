using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SubscriptionSystem.Domain.Entities
{
    public class User
    {
        [Key]
        public string Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }
        public string? EmailConfirmationOTP { get; set; }
        public DateTime? EmailConfirmationOTPExpiry { get; set; }
        public string? PasswordResetOTP { get; set; }
        public DateTime? SubscriptionExpiry { get; set; }
        public DateTime? PasswordResetOTPExpiry { get; set; }
        public string? EmailChangeOTP { get; set; }
        public DateTime? EmailChangeOTPExpiry { get; set; }
        public bool HasActiveSubscription { get; set; }
        public string? NewEmail { get; set; }
        public bool AccountDeletionRequested { get; set; }
        public DateTime? AccountDeletionRequestDate { get; set; }
        public bool EnableNotifications { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public string? TwoFactorSecret { get; set; }
        public string? AccountDeletionReason { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string FullName { get; set; } // Add this property
        public decimal SubscriptionAmount { get; set; } // Add this property
        [JsonIgnore]
        public virtual ICollection<Subscription> Subscriptions { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
        public ICollection<ApiKey> ApiKeys { get; set; }
        public ICollection<Ticket> Tickets { get; set; }
        public string? GoogleId { get; set; }
        public string? ProfilePicture { get; set; }
    public bool MustChangePassword { get; set; }
    // Allow null unless user signs up via USSD channel
    public string? TemporaryPassword { get; set; }
    }

}


