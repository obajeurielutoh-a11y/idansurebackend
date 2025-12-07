using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;
using System.Text;

namespace SubscriptionSystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaystackWebhookController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaystackWebhookController(IPaymentService paymentService, IConfiguration configuration)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                // Read the request body
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var payload = await reader.ReadToEndAsync();

                // Verify the signature
                var signature = Request.Headers["X-Paystack-Signature"].ToString();
                if (!_paymentService.VerifyPaystackWebhookSignature(payload, signature))
                {
                    return Unauthorized(new { message = "Invalid signature" });
                }

                // Deserialize the webhook data
                var webhookData = System.Text.Json.JsonSerializer.Deserialize<PaystackWebhookDto>(payload, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Only process charge.success events
                if (webhookData.Event != "charge.success")
                {
                    return Ok(new { message = "Event ignored" });
                }

                // Process the webhook
                var result = await _paymentService.ProcessUnifiedWebhookAsync(webhookData, "paystack");

                if (result.IsSuccess)
                {
                    return Ok(new { message = "Webhook processed successfully", transactionId = result.Data.Id });
                }
                else
                {
                    return BadRequest(new { message = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"An error occurred: {ex.Message}" });
            }
        }
    }
}
