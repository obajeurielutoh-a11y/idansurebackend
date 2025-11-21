using System;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Domain.Entities
{
    public class VerifiedEmail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }
}

