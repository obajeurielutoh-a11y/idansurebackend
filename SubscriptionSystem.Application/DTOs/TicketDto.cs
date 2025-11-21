using System;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.DTOs
{
    public class TicketDto
    {
        public Guid Id { get; set; }
        public string Subject { get; set; }
        public string MessageBody { get; set; }
        public DateTime CreatedAt { get; set; }
        public TicketStatus Status { get; set; }
        public string UserId { get; set; }
    }

    public class CreateTicketDto
    {
        public string Subject { get; set; }
        public string MessageBody { get; set; }
        public string UserId { get; set; }
    }

    public class UpdateTicketStatusDto
    {
        public Guid TicketId { get; set; }
        public TicketStatus NewStatus { get; set; }
        public string UserId { get; set; }
    }
}

