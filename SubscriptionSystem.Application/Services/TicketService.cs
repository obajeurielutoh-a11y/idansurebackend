using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Application.Services
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IEmailService _emailService;
        private readonly IUserManagementService _userService;
        private readonly ILogger<TicketService> _logger;

        public TicketService(
            ITicketRepository ticketRepository,
            IEmailService emailService,
            IUserManagementService userService,
            ILogger<TicketService> logger)
        {
            _ticketRepository = ticketRepository;
            _emailService = emailService;
            _userService = userService;
            _logger = logger;
        }

        public async Task<ServiceResult<TicketDto>> CreateTicketAsync(CreateTicketDto createTicketDto)
        {
            try
            {
                var ticket = new Ticket
                {
                    Id = Guid.NewGuid(),
                    Subject = createTicketDto.Subject,
                    MessageBody = createTicketDto.MessageBody,
                    CreatedAt = DateTime.UtcNow,
                    Status = TicketStatus.Open,
                    UserId = createTicketDto.UserId
                };

                var createdTicket = await _ticketRepository.CreateTicketAsync(ticket);
                _logger.LogInformation($"Ticket created successfully. Ticket ID: {createdTicket.Id}");

                await SendTicketCreationEmail(createdTicket);

                return new ServiceResult<TicketDto>
                {
                    Data = MapToDto(createdTicket),
                    IsSuccess = true,
                    Message = "Ticket created successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating a ticket");
                return new ServiceResult<TicketDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while creating the ticket: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<TicketDto>> GetTicketByIdAsync(Guid id)
        {
            try
            {
                var ticket = await _ticketRepository.GetTicketByIdAsync(id);
                if (ticket == null)
                {
                    return new ServiceResult<TicketDto>
                    {
                        IsSuccess = false,
                        Message = "Ticket not found."
                    };
                }

                return new ServiceResult<TicketDto>
                {
                    Data = MapToDto(ticket),
                    IsSuccess = true,
                    Message = "Ticket retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while retrieving ticket with ID: {id}");
                return new ServiceResult<TicketDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving the ticket: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<PaginatedResult<TicketDto>>> GetUserTicketsAsync(string userId, int page, int pageSize)
        {
            try
            {
                var paginatedResult = await _ticketRepository.GetUserTicketsAsync(userId, page, pageSize);
                var ticketDtos = paginatedResult.Items.Select(MapToDto).ToList();

                var paginatedDtoResult = new PaginatedResult<TicketDto>
                {
                    Items = ticketDtos,
                    TotalCount = paginatedResult.TotalCount,
                    Page = paginatedResult.Page,
                    PageSize = paginatedResult.PageSize
                };

                _logger.LogInformation($"Retrieved {ticketDtos.Count} tickets for user {userId}");
                return new ServiceResult<PaginatedResult<TicketDto>>
                {
                    Data = paginatedDtoResult,
                    IsSuccess = true,
                    Message = "Tickets retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while retrieving tickets for user {userId}");
                return new ServiceResult<PaginatedResult<TicketDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving tickets: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<TicketDto>> UpdateTicketStatusAsync(UpdateTicketStatusDto updateTicketStatusDto)
        {
            try
            {
                var ticket = await _ticketRepository.GetTicketByIdAsync(updateTicketStatusDto.TicketId);
                if (ticket == null)
                {
                    _logger.LogWarning($"Ticket not found. Ticket ID: {updateTicketStatusDto.TicketId}");
                    return new ServiceResult<TicketDto>
                    {
                        IsSuccess = false,
                        Message = "Ticket not found."
                    };
                }

                if (ticket.UserId != updateTicketStatusDto.UserId)
                {
                    _logger.LogWarning($"Unauthorized attempt to update ticket. Ticket ID: {updateTicketStatusDto.TicketId}, User ID: {updateTicketStatusDto.UserId}");
                    return new ServiceResult<TicketDto>
                    {
                        IsSuccess = false,
                        Message = "Unauthorized to update this ticket."
                    };
                }

                var oldStatus = ticket.Status;
                ticket.Status = updateTicketStatusDto.NewStatus;
                var updatedTicket = await _ticketRepository.UpdateTicketAsync(ticket);
                var ticketResponse = MapToDto(updatedTicket);

                _logger.LogInformation($"Ticket status updated successfully. Ticket ID: {updateTicketStatusDto.TicketId}, New Status: {updateTicketStatusDto.NewStatus}");

                await SendTicketStatusUpdateEmail(updatedTicket, oldStatus);

                return new ServiceResult<TicketDto>
                {
                    Data = ticketResponse,
                    IsSuccess = true,
                    Message = "Ticket status updated successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating ticket status. Ticket ID: {updateTicketStatusDto.TicketId}");
                return new ServiceResult<TicketDto>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while updating the ticket status: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<bool>> DeleteTicketAsync(Guid id, string userId)
        {
            try
            {
                var ticket = await _ticketRepository.GetTicketByIdAsync(id);
                if (ticket == null)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        Message = "Ticket not found."
                    };
                }

                if (ticket.UserId != userId)
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        Message = "Unauthorized to delete this ticket."
                    };
                }

                var result = await _ticketRepository.DeleteTicketAsync(id);
                if (result)
                {
                    _logger.LogInformation($"Ticket deleted successfully. Ticket ID: {id}");
                    return new ServiceResult<bool>
                    {
                        Data = true,
                        IsSuccess = true,
                        Message = "Ticket deleted successfully."
                    };
                }
                else
                {
                    return new ServiceResult<bool>
                    {
                        IsSuccess = false,
                        Message = "Failed to delete the ticket."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting ticket. Ticket ID: {id}");
                return new ServiceResult<bool>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while deleting the ticket: {ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<PaginatedResult<AdminTicketDto>>> GetAllTicketsForAdminAsync(int page, int pageSize)
        {
            try
            {
                var paginatedResult = await _ticketRepository.GetAllTicketsAsync(page, pageSize);
                var adminTicketDtos = new List<AdminTicketDto>();

                foreach (var ticket in paginatedResult.Items)
                {
                    var userEmail = await _userService.GetUserEmailAsync(ticket.UserId);
                    adminTicketDtos.Add(new AdminTicketDto
                    {
                        Id = ticket.Id,
                        Subject = ticket.Subject,
                        MessageBody = ticket.MessageBody,
                        CreatedAt = ticket.CreatedAt,
                        Status = ticket.Status,
                        UserId = ticket.UserId,
                        UserEmail = userEmail
                    });
                }

                var paginatedDtoResult = new PaginatedResult<AdminTicketDto>
                {
                    Items = adminTicketDtos,
                    TotalCount = paginatedResult.TotalCount,
                    Page = paginatedResult.Page,
                    PageSize = paginatedResult.PageSize
                };

                _logger.LogInformation($"Retrieved {adminTicketDtos.Count} tickets for admin");
                return new ServiceResult<PaginatedResult<AdminTicketDto>>
                {
                    Data = paginatedDtoResult,
                    IsSuccess = true,
                    Message = "Tickets retrieved successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving tickets for admin");
                return new ServiceResult<PaginatedResult<AdminTicketDto>>
                {
                    IsSuccess = false,
                    Message = $"An error occurred while retrieving tickets: {ex.Message}"
                };
            }
        }

        private TicketDto MapToDto(Ticket ticket)
        {
            return new TicketDto
            {
                Id = ticket.Id,
                Subject = ticket.Subject,
                MessageBody = ticket.MessageBody,
                CreatedAt = ticket.CreatedAt,
                Status = ticket.Status,
                UserId = ticket.UserId
            };
        }

        private async Task SendTicketCreationEmail(Ticket ticket)
        {
            var subject = $"New Ticket Created - {ticket.Subject}";
            var body = $@"
                <h2>New Ticket Created</h2>
                <p>A new ticket has been created with the following details:</p>
                <ul>
                    <li><strong>Ticket ID:</strong> {ticket.Id}</li>
                    <li><strong>Subject:</strong> {ticket.Subject}</li>
                    <li><strong>Status:</strong> {ticket.Status}</li>
                    <li><strong>Created At:</strong> {ticket.CreatedAt}</li>
                </ul>
                <p>Message:</p>
                <p>{ticket.MessageBody}</p>
            ";

            var userEmail = await GetUserEmailAsync(ticket.UserId);
            await _emailService.SendEmailAsync(userEmail, subject, body);
        }

        private async Task SendTicketStatusUpdateEmail(Ticket ticket, TicketStatus oldStatus)
        {
            var subject = ticket.Status == TicketStatus.Closed
                ? $"Ticket Closed - {ticket.Subject}"
                : $"Ticket Status Updated - {ticket.Subject}";

            string body;
            if (ticket.Status == TicketStatus.Closed)
            {
                body = $@"
                    <h2>Ticket Closed</h2>
                    <p>Your ticket has been closed:</p>
                    <ul>
                        <li><strong>Ticket ID:</strong> {ticket.Id}</li>
                        <li><strong>Subject:</strong> {ticket.Subject}</li>
                    </ul>
                    <p>Thank you for your patience. Please log in to continue using our services.</p>
                    <p>If you have any further questions or concerns, please create a new ticket.</p>
                    <p>We appreciate your business!</p>
                ";
            }
            else
            {
                body = $@"
                    <h2>Ticket Status Updated</h2>
                    <p>The status of your ticket has been updated:</p>
                    <ul>
                        <li><strong>Ticket ID:</strong> {ticket.Id}</li>
                        <li><strong>Subject:</strong> {ticket.Subject}</li>
                        <li><strong>Old Status:</strong> {oldStatus}</li>
                        <li><strong>New Status:</strong> {ticket.Status}</li>
                        <li><strong>Updated At:</strong> {DateTime.UtcNow}</li>
                    </ul>
                ";
            }

            var userEmail = await GetUserEmailAsync(ticket.UserId);
            await _emailService.SendEmailAsync(userEmail, subject, body);
        }

        private async Task<string> GetUserEmailAsync(string userId)
        {
            try
            {
                return await _userService.GetUserEmailAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while retrieving email for user {userId}");
                throw;
            }
        }
    }
}

