using System;
using System.Threading.Tasks;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface ITicketService
    {
        Task<ServiceResult<TicketDto>> CreateTicketAsync(CreateTicketDto createTicketDto);
        Task<ServiceResult<TicketDto>> GetTicketByIdAsync(Guid id);
        Task<ServiceResult<PaginatedResult<TicketDto>>> GetUserTicketsAsync(string userId, int page, int pageSize);
        Task<ServiceResult<TicketDto>> UpdateTicketStatusAsync(UpdateTicketStatusDto updateTicketStatusDto);
        Task<ServiceResult<bool>> DeleteTicketAsync(Guid id, string userId);
        Task<ServiceResult<PaginatedResult<AdminTicketDto>>> GetAllTicketsForAdminAsync(int page, int pageSize);
    }
}

