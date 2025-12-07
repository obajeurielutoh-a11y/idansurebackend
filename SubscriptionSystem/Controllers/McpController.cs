using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System.Text.Json;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("mcp")] // Minimal MCP-like JSON-RPC endpoint
    public class McpController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentService _paymentService;
        private readonly IDomainEventPublisher _eventPublisher;
        private readonly ILogger<McpController> _logger;

        public McpController(ISubscriptionService subscriptionService, IPaymentService paymentService, IDomainEventPublisher eventPublisher, ILogger<McpController> logger)
        {
            _subscriptionService = subscriptionService;
            _paymentService = paymentService;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public class RpcRequest
        {
            public string? Id { get; set; }
            public string Method { get; set; } = string.Empty;
            public JsonElement Params { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Handle([FromBody] RpcRequest req)
        {
            try
            {
                switch (req.Method)
                {
                    case "checkSubscription":
                        {
                            var userId = req.Params.GetProperty("userId").GetString()!;
                            var active = await _subscriptionService.HasActiveSubscriptionAsync(userId);
                            return Ok(new { id = req.Id, result = new { active } });
                        }
                    case "createPaymentIntent":
                        {
                            // Stub: here you could forward to your payment provider to create intent
                            var amount = req.Params.GetProperty("amount").GetDecimal();
                            var currency = req.Params.TryGetProperty("currency", out var cur) ? cur.GetString() : "NGN";
                            var txRef = Guid.NewGuid().ToString();
                            return Ok(new { id = req.Id, result = new { transactionRef = txRef, amount, currency } });
                        }
                    case "postTipNotification":
                        {
                            // Broadcast a TipPostedEvent (useful for external admins via MCP)
                            var predictionId = req.Params.GetProperty("predictionId").GetGuid();
                            var tournament = req.Params.GetProperty("tournament").GetString() ?? string.Empty;
                            var team1 = req.Params.GetProperty("team1").GetString() ?? string.Empty;
                            var team2 = req.Params.GetProperty("team2").GetString() ?? string.Empty;
                            var matchDate = req.Params.TryGetProperty("matchDate", out var md) && md.ValueKind == JsonValueKind.String && DateTime.TryParse(md.GetString(), out var parsed)
                                ? parsed : DateTime.UtcNow;

                            await _eventPublisher.PublishAsync(new SubscriptionSystem.Domain.Events.TipPostedEvent(
                                predictionId, true, false, matchDate, tournament, team1, team2));
                            return Ok(new { id = req.Id, result = new { ok = true } });
                        }
                    default:
                        return BadRequest(new { id = req.Id, error = new { code = -32601, message = "Method not found" } });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP handler error");
                return StatusCode(500, new { id = req.Id, error = new { code = -32000, message = ex.Message } });
            }
        }
    }
}
