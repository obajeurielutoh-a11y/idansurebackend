using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace SubscriptionSystem.API.Controllers
{
    /// <summary>
    /// WhatsApp Integration Controller
    /// Handles WhatsApp number registration, verification, and PraisonAI Agent coordination
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WhatsAppIntegrationController : ControllerBase
    {
        private readonly PraisonAIWhatsAppAgentService _whatsAppAgentService;
        private readonly WhatsAppMCPAdapter _mcpAdapter;
        private readonly ILogger<WhatsAppIntegrationController> _logger;

        public WhatsAppIntegrationController(
            PraisonAIWhatsAppAgentService whatsAppAgentService,
            WhatsAppMCPAdapter mcpAdapter,
            ILogger<WhatsAppIntegrationController> logger)
        {
            _whatsAppAgentService = whatsAppAgentService;
            _mcpAdapter = mcpAdapter;
            _logger = logger;
        }

        /// <summary>
        /// Register and verify a WhatsApp number for the user
        /// Uses PraisonAI Agent to verify via WhatsApp MCP
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterWhatsAppNumber([FromBody] RegisterWhatsAppRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest(new { message = "Phone number is required" });

            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { message = "User ID not found in token" });

                _logger.LogInformation("Registering WhatsApp number for user {UserId}: {PhoneNumber}", userId, request.PhoneNumber);

                // Validate phone number format
                var validation = _whatsAppAgentService.ValidateUserWhatsAppRegistration(userId, request.PhoneNumber);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid WhatsApp number",
                        errors = validation.Errors
                    });
                }

                // Verify using PraisonAI Agent + WhatsApp MCP
                var isVerified = await _whatsAppAgentService.VerifyWhatsAppNumberAsync(validation.NormalizedPhoneNumber);

                if (!isVerified)
                {
                    return BadRequest(new
                    {
                        message = "Failed to verify WhatsApp number. Please ensure the number is registered with WhatsApp and has enabled notifications.",
                        help = "Supported formats: +2348012345678 or 08012345678"
                    });
                }

                // Create MCP Resource for verified recipient
                var mcpRecipient = _mcpAdapter.CreateRecipientMCP(
                    validation.NormalizedPhoneNumber ?? "",
                    request.PreferredLanguage ?? "en",
                    new System.Collections.Generic.Dictionary<string, object>
                    {
                        { "userId", userId },
                        { "registeredAt", DateTime.UtcNow }
                    }
                );

                // Verify in MCP context
                var mcpVerified = await _mcpAdapter.VerifyRecipientMCPAsync(mcpRecipient);

                if (!mcpVerified)
                {
                    return StatusCode(500, new
                    {
                        message = "MCP verification failed. Please try again.",
                        error = "WhatsApp MCP adapter verification failed"
                    });
                }

                _logger.LogInformation(
                    "WhatsApp number registered and verified for user {UserId}: {Phone}",
                    userId,
                    validation.NormalizedPhoneNumber);

                return Ok(new
                {
                    message = "WhatsApp number registered and verified successfully",
                    phoneNumber = validation.NormalizedPhoneNumber,
                    language = request.PreferredLanguage ?? "en",
                    mcpUri = mcpRecipient.Uri,
                    notificationsEnabled = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register WhatsApp number");
                return StatusCode(500, new { message = "Failed to register WhatsApp number", error = ex.Message });
            }
        }

        /// <summary>
        /// Update existing WhatsApp registration
        /// </summary>
        [HttpPut("update")]
        public async Task<IActionResult> UpdateWhatsAppNumber([FromBody] UpdateWhatsAppRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest(new { message = "Phone number is required" });

            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { message = "User ID not found in token" });

                _logger.LogInformation("Updating WhatsApp number for user {UserId}", userId);

                // Validate new number
                var validation = _whatsAppAgentService.ValidateUserWhatsAppRegistration(userId, request.PhoneNumber);
                if (!validation.IsValid)
                {
                    return BadRequest(new
                    {
                        message = "Invalid WhatsApp number",
                        errors = validation.Errors
                    });
                }

                // Verify new number
                var isVerified = await _whatsAppAgentService.VerifyWhatsAppNumberAsync(validation.NormalizedPhoneNumber);
                if (!isVerified)
                {
                    return BadRequest(new { message = "Failed to verify new WhatsApp number" });
                }

                _logger.LogInformation(
                    "WhatsApp number updated for user {UserId}: {Phone}",
                    userId,
                    validation.NormalizedPhoneNumber);

                return Ok(new
                {
                    message = "WhatsApp number updated successfully",
                    phoneNumber = validation.NormalizedPhoneNumber,
                    language = request.PreferredLanguage ?? "en",
                    notificationsEnabled = request.EnableNotifications ?? true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update WhatsApp number");
                return StatusCode(500, new { message = "Failed to update WhatsApp number", error = ex.Message });
            }
        }

        /// <summary>
        /// Verify WhatsApp number without registration
        /// Useful for testing/validation
        /// </summary>
        [HttpPost("verify")]
        public async Task<IActionResult> VerifyWhatsAppNumber([FromBody] VerifyWhatsAppRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.PhoneNumber))
                return BadRequest(new { message = "Phone number is required" });

            try
            {
                var normalized = _whatsAppAgentService.NormalizeToE164(request.PhoneNumber);
                if (string.IsNullOrEmpty(normalized))
                {
                    return BadRequest(new
                    {
                        message = "Invalid phone number format",
                        help = "Use format: +2348012345678 or 08012345678"
                    });
                }

                var isVerified = await _whatsAppAgentService.VerifyWhatsAppNumberAsync(normalized);

                return Ok(new
                {
                    phoneNumber = request.PhoneNumber,
                    normalizedPhoneNumber = normalized,
                    isVerified = isVerified,
                    message = isVerified
                        ? "WhatsApp number is valid and ready to receive notifications"
                        : "WhatsApp number verification failed. Please check the number is correct."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WhatsApp verification failed");
                return StatusCode(500, new { message = "Verification failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Get WhatsApp MCP resource information
        /// </summary>
        [HttpGet("mcp/{resourceUri}")]
        public IActionResult GetMCPResource(string resourceUri)
        {
            try
            {
                var resource = _mcpAdapter.GetResourceByUri(resourceUri);
                if (resource == null)
                    return NotFound(new { message = "MCP resource not found" });

                return Ok(new
                {
                    uri = resourceUri,
                    resource = resource,
                    message = "MCP resource retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get MCP resource");
                return StatusCode(500, new { message = "Failed to retrieve MCP resource", error = ex.Message });
            }
        }

        /// <summary>
        /// Send test notification to verify WhatsApp registration
        /// </summary>
        [HttpPost("test-notification")]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("userId")?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                    return Unauthorized(new { message = "User ID not found in token" });

                _logger.LogInformation("Sending test notification for user {UserId}", userId);

                // In production, would get user's WhatsApp number from database
                var testMessage = @"🎯 IdanSure Test Notification

Hello! This is a test message to verify your WhatsApp registration is working correctly.

If you receive this message, you're all set to get prediction updates! 🎉

Best of luck with your bets! 💪";

                return Ok(new
                {
                    message = "Test notification functionality is ready",
                    testMessagePreview = testMessage,
                    note = "To receive actual test notifications, register your WhatsApp number first using /register endpoint"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send test notification");
                return StatusCode(500, new { message = "Failed to send test notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Get WhatsApp integration status
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult GetIntegrationStatus()
        {
            return Ok(new
            {
                message = "WhatsApp Integration Status",
                status = "operational",
                features = new
                {
                    registration = "enabled",
                    verification = "enabled",
                    mcpAdapter = "active",
                    praisonAIIntegration = "enabled",
                    supportedLanguages = new[] { "en", "ig", "ha", "yo", "pcm" }
                },
                endpoints = new
                {
                    register = "POST /api/whatsappintegration/register",
                    update = "PUT /api/whatsappintegration/update",
                    verify = "POST /api/whatsappintegration/verify",
                    testNotification = "POST /api/whatsappintegration/test-notification",
                    mcpResource = "GET /api/whatsappintegration/mcp/{resourceUri}"
                }
            });
        }
    }

    /// <summary>
    /// Request DTO for WhatsApp registration
    /// </summary>
    public class RegisterWhatsAppRequestDto
    {
        public string? PhoneNumber { get; set; }
        public string? PreferredLanguage { get; set; }
    }

    /// <summary>
    /// Request DTO for WhatsApp update
    /// </summary>
    public class UpdateWhatsAppRequestDto
    {
        public string? PhoneNumber { get; set; }
        public string? PreferredLanguage { get; set; }
        public bool? EnableNotifications { get; set; }
    }

    /// <summary>
    /// Request DTO for WhatsApp verification
    /// </summary>
    public class VerifyWhatsAppRequestDto
    {
        public string? PhoneNumber { get; set; }
    }
}
