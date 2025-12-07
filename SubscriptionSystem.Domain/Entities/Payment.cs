using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubscriptionSystem.Domain.Entities
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public string Status { get; set; }

        [Required]
        public string TransactionId { get; set; }

        public DateTime PaymentDate { get; set; }

    }
}

