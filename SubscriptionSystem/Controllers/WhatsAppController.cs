using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SubscriptionSystem.Infrastructure.Services;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Events;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly PredictionNotificationService _notificationService;
        private readonly IDomainEventPublisher _eventPublisher;
        private readonly ILogger<WhatsAppController> _logger;

        public WhatsAppController(PredictionNotificationService notificationService, IDomainEventPublisher eventPublisher, ILogger<WhatsAppController> logger)
        {
            _notificationService = notificationService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Send a test WhatsApp notification to the specified phone number (E.164 or local format).
        /// Useful for verifying outbound WhatsApp config without needing subscribers.
        /// </summary>
        [HttpPost("test")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> SendTest([FromBody] TestWhatsAppDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return BadRequest(new { message = "PhoneNumber is required" });

            try
            {
                await _notificationService.SendTestNotificationAsync(dto.PhoneNumber);
                return Ok(new { message = "Test WhatsApp message dispatched (check logs/delivery)." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending test WhatsApp message to {Phone}", dto.PhoneNumber);
                return StatusCode(500, new { message = "Failed to send test message", details = ex.Message });
            }
        }

        /// <summary>
        /// Replay/publish a TipPostedEvent to trigger subscriber notifications.
        /// This lets you test the end-to-end flow (event -> handler -> WhatsApp) without creating a real prediction.
        /// </summary>
        [HttpPost("replay")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> ReplayTip([FromBody] ReplayTipDto dto)
        {
            if (dto == null)
                return BadRequest(new { message = "Payload required" });

            try
            {
                var evt = new TipPostedEvent(Guid.NewGuid(), dto.IsDetailed, dto.IsPromotional, dto.MatchDate ?? DateTime.UtcNow, dto.Tournament ?? string.Empty, dto.Team1 ?? string.Empty, dto.Team2 ?? string.Empty);
                await _eventPublisher.PublishAsync(evt);
                return Ok(new { message = "TipPostedEvent published" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing TipPostedEvent");
                return StatusCode(500, new { message = "Failed to publish event", details = ex.Message });
            }
        }
    }

    public class TestWhatsAppDto
    {
        public string PhoneNumber { get; set; }
    }

    public class ReplayTipDto
    {
        public bool IsDetailed { get; set; } = false;
        public bool IsPromotional { get; set; } = false;
        public DateTime? MatchDate { get; set; }
        public string? Tournament { get; set; }
        public string? Team1 { get; set; }
        public string? Team2 { get; set; }
    }
}
