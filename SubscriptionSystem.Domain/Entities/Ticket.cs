using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubscriptionSystem.Domain.Entities
{
    public class Ticket
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; }

        [Required]
        public string MessageBody { get; set; }

        public DateTime CreatedAt { get; set; }

        public TicketStatus Status { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }

    public enum TicketStatus
    {
        Open,
        InProgress,
        Closed
    }
}

