using System;
using System.Threading.Tasks;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface ITicketRepository
    {
        Task<Ticket> CreateTicketAsync(Ticket ticket);
        Task<Ticket> GetTicketByIdAsync(Guid id);
        Task<PaginatedResult<Ticket>> GetUserTicketsAsync(string userId, int page, int pageSize);
        Task<Ticket> UpdateTicketAsync(Ticket ticket);
        Task<bool> DeleteTicketAsync(Guid id);
        Task<PaginatedResult<Ticket>> GetAllTicketsAsync(int page, int pageSize);
    }
}

