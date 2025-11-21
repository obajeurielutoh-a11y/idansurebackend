using SubscriptionSystem.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SubscriptionSystem.Domain.Entities
{
    public class Subscription
    {
        [Key]
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual User User { get; set; }

        public bool IsActive { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public string? TransactionId { get; set; }
        public string? Email { get; set; }
        public decimal AmountPaid { get; set; }
        public string? Currency { get; set; }
        public string? CustomerRef { get; set; }
        public string? TransactionReference { get; set; }
        public string PaymentGateway { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
     
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public SubscriptionPlan Plan { get; set; }

        public string? PhoneNumber { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime StartDate { get; set; }
        public int PaymentFailures { get; set; }
        public SubscriptionStatuses Status { get; set; }

        // Add this property
        public string PlanType
        {
            get => Plan.ToString();
            set
            {
                if (Enum.TryParse<SubscriptionPlan>(value, true, out var result))
                {
                    Plan = result;
                }
                else
                {
                    throw new ArgumentException($"Invalid plan type: {value}");
                }
            }
        }
    }

    public enum SubscriptionPlan
    {
        OneDay = 1,
        OneWeek = 7,
        OneMonth = 31
    }

}

