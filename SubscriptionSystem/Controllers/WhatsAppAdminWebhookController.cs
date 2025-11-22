using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text;
using System.Security.Cryptography;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppAdminWebhookController : ControllerBase
    {
        private readonly WhatsAppAdminPredictionService _adminService;
        private readonly ILogger<WhatsAppAdminWebhookController> _logger;
        private readonly IConfiguration _configuration;

        public WhatsAppAdminWebhookController(WhatsAppAdminPredictionService adminService, ILogger<WhatsAppAdminWebhookController> logger, IConfiguration configuration)
        {
            _adminService = adminService;
            _logger = logger;
            _configuration = configuration;
        }

        // Minimal DTO representing incoming WhatsApp webhook payload.
        public class WhatsAppIncomingDto
        {
            public string From { get; set; } = string.Empty;
            public string Text { get; set; } = string.Empty;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Receive()
        {
            // 1) Verify HMAC signature if configured
            var secret = _configuration["WhatsApp:WebhookSecret"]; // set this to a strong secret

            // Enable rewind to read raw body
            Request.EnableBuffering();
            using var sr = new System.IO.StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await sr.ReadToEndAsync();
            Request.Body.Position = 0;

            // Support rotated secrets: comma-separated or array in config
            var secretsConfig = _configuration.GetSection("WhatsApp:WebhookSecrets").Get<string[]>();
            string[] secrets;
            if (secretsConfig != null && secretsConfig.Length > 0)
                secrets = secretsConfig;
            else if (!string.IsNullOrEmpty(secret))
                secrets = new[] { secret };
            else
                secrets = Array.Empty<string>();

            if (secrets.Length > 0)
            {
                // Facebook/WhatsApp uses header 'X-Hub-Signature-256' with value 'sha256=...'
                var sigHeader = Request.Headers["X-Hub-Signature-256"].ToString();
                if (string.IsNullOrEmpty(sigHeader))
                    sigHeader = Request.Headers["X-WhatsApp-Signature"].ToString();

                if (string.IsNullOrEmpty(sigHeader))
                {
                    _logger.LogWarning("Missing signature header for incoming webhook");
                    return Unauthorized("Missing signature");
                }

                // Normalize header value
                var prefix = "sha256=";
                if (sigHeader.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    sigHeader = sigHeader.Substring(prefix.Length);

                var valid = false;
                var incomingHex = sigHeader.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase)
                    ? sigHeader.Substring("sha256=".Length).Trim().ToLowerInvariant()
                    : sigHeader.Trim().ToLowerInvariant();

                foreach (var s in secrets)
                {
                    try
                    {
                        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(s));
                        var computed = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
                        var computedHex = BitConverter.ToString(computed).Replace("-", "").ToLowerInvariant();
                        var computedBytes = Convert.FromHexString(computedHex);
                        var incomingBytes = Convert.FromHexString(incomingHex);
                        if (CryptographicOperations.FixedTimeEquals(computedBytes, incomingBytes))
                        {
                            valid = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Signature verification try failed for one secret");
                        // try next secret
                    }
                }

                if (!valid)
                {
                    _logger.LogWarning("Webhook signature mismatch for all active secrets");
                    return Unauthorized("Invalid signature");
                }
            }

            // 2) Replay protection: compute body hash and check cache
            var bodyHash = SubscriptionSystem.Infrastructure.Services.WebhookReplayProtector.ComputeBodyHashHex(body);
            var protector = HttpContext.RequestServices.GetService(typeof(SubscriptionSystem.Infrastructure.Services.WebhookReplayProtector)) as SubscriptionSystem.Infrastructure.Services.WebhookReplayProtector;
            if (protector != null)
            {
                var isReplay = await protector.IsReplayAsync(bodyHash);
                if (isReplay)
                {
                    _logger.LogWarning("Detected replayed webhook payload (hash={Hash})", bodyHash);
                    return Conflict(new { message = "Duplicate webhook payload" });
                }
            }

            // 3) Deserialize payload (we read raw body intentionally)
            WhatsAppIncomingDto? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<WhatsAppIncomingDto>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize webhook payload");
                return BadRequest("Invalid payload");
            }

            if (payload == null)
                return BadRequest("payload required");

            _logger.LogInformation("Received admin whatsapp webhook from {From}", payload.From);
            var res = await _adminService.HandleIncomingAsync(payload.From, payload.Text ?? string.Empty);
            // Mark processed on success to prevent replays
            if (protector != null && res.IsSuccess)
            {
                await protector.MarkProcessedAsync(bodyHash);
            }
            if (res.IsSuccess)
                return Ok(new { message = res.Message });
            return BadRequest(new { message = res.Message });
        }
    }
}
