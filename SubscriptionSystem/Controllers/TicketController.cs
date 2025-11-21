using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketController : ControllerBase
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<TicketController> _logger;

        public TicketController(ITicketService ticketService, ILogger<TicketController> logger)
        {
            _ticketService = ticketService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> CreateTicket([FromBody] CreateTicketDto createTicketDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _ticketService.CreateTicketAsync(createTicketDto);
            if (result.IsSuccess)
            {
                return CreatedAtAction(nameof(GetTicket), new { id = result.Data.Id }, result.Data);
            }

            return BadRequest(result.Message);
        }

        [HttpGet("{id}")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> GetTicket(Guid id)
        {
            var result = await _ticketService.GetTicketByIdAsync(id);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return NotFound(result.Message);
        }

        [HttpGet("user/{userId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetUserTickets(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _ticketService.GetUserTicketsAsync(userId, page, pageSize);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.Message);
        }

        [HttpPut("status")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> UpdateTicketStatus([FromBody] UpdateTicketStatusDto updateTicketStatusDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _ticketService.UpdateTicketStatusAsync(updateTicketStatusDto);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.Message);
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> DeleteTicket(Guid id, [FromQuery] string userId)
        {
            var result = await _ticketService.DeleteTicketAsync(id, userId);
            if (result.IsSuccess)
            {
                return NoContent();
            }

            return BadRequest(result.Message);
        }
        [HttpGet("admin/all")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> GetAllTicketsForAdmin([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var result = await _ticketService.GetAllTicketsForAdminAsync(page, pageSize);
            if (result.IsSuccess)
            {
                return Ok(result.Data);
            }

            return BadRequest(result.Message);
        }
    }
}

