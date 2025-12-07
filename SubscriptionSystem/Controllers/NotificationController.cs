using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.DTOs;
using System.Xml.Linq;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController : ControllerBase
    {
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ILogger<NotificationController> logger)
        {
            _logger = logger;
        }

        [HttpPost("ussd")]
        [Consumes("application/xml")]
        public IActionResult ReceiveUssdNotification([FromBody] UssdNotificationDto notification)
        {
            if (notification == null) return BadRequest("Invalid XML payload.");

            _logger.LogInformation("Received USSD Notification: Shortcode={Shortcode}, Msisdn={Msisdn}, Choice={Choice}",
                notification.Shortcode, notification.Msisdn, notification.Choice);

            return Ok("USSD Notification received.");
        }

        [HttpPost("delivery")]
        [Consumes("application/xml")]
        public IActionResult ReceiveDeliveryNotification([FromBody] DeliveryNotificationDto notification)
        {
            if (notification == null) return BadRequest("Invalid XML payload.");

            _logger.LogInformation("Received Delivery Notification: Shortcode={Shortcode}, Msisdn={Msisdn}, Status={Status}",
                notification.Shortcode, notification.Msisdn, notification.DeliveryStatus);

            return Ok("Delivery Notification received.");
        }

        [HttpPost("datsync")]
        public async Task<IActionResult> ReceiveDatsyncNotification()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                if (string.IsNullOrWhiteSpace(body)) return BadRequest("Empty body");

                var doc = XDocument.Parse(body);
                // Basic extraction logic - robust to namespaces by using LocalName
                var respDto = doc.Descendants()
                    .FirstOrDefault(x => x.Name.LocalName == "notificationRespDTO");

                if (respDto != null)
                {
                    var amount = respDto.Elements().FirstOrDefault(x => x.Name.LocalName == "amount")?.Value;
                    var msisdn = respDto.Elements().FirstOrDefault(x => x.Name.LocalName == "msisdn")?.Value;
                    var errorMsg = respDto.Elements().FirstOrDefault(x => x.Name.LocalName == "errorMsg")?.Value;

                    _logger.LogInformation("Received Datsync Notification: Msisdn={Msisdn}, Amount={Amount}, Msg={Msg}",
                        msisdn, amount, errorMsg);
                }
                else
                {
                    _logger.LogWarning("Received Datsync Notification but could not find notificationRespDTO");
                }

                // SOAP response often expected
                var soapResponse = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""><soapenv:Body><notificationResponse>OK</notificationResponse></soapenv:Body></soapenv:Envelope>";
                return Content(soapResponse, "text/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Datsync notification");
                return StatusCode(500, "Internal Error");
            }
        }
    }
}
