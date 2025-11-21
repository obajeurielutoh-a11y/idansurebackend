using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Common;
using SubscriptionSystem.Domain.Entities;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace SubscriptionSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IPaymentService paymentService,
            IConfiguration config,
            IHttpClientFactory httpClientFactory)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
        }

        // ---------------------------- EXISTING ENDPOINTS ----------------------------
        [HttpPost("paystack/initialize")]
        public async Task<IActionResult> InitializePaystackPayment([FromBody] PaystackInitializeRequestDto request)
        {
            if (request == null)
                return BadRequest("Invalid payment request.");

            var result = await _paymentService.InitializePaystackPaymentAsync(request);

            if (result.IsSuccess)
                return Ok(result.Data);

            return BadRequest(new { message = result.ErrorMessage });
        }

        [HttpGet("paystack/verify/{reference}")]
        public async Task<IActionResult> VerifyPaystackPayment(string reference)
        {
            if (string.IsNullOrEmpty(reference))
                return BadRequest("Reference cannot be empty.");

            var result = await _paymentService.VerifyPaystackPaymentAsync(reference);
            return result.IsSuccess ? Ok(result.Data) : BadRequest(new { message = result.ErrorMessage });
        }

        [HttpPost("notification")]
        [Authorize(AuthenticationSchemes = "Basic")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentNotificationDto notification)
        {
            try
            {
                var result = await _paymentService.ProcessUnifiedWebhookAsync(notification, "coralpay");

                if (result.IsSuccess)
                    return Ok(new PaymentResponse { ResponseCode = "00", ResponseMessage = "Payment processed successfully" });

                return BadRequest(new PaymentResponse { ResponseCode = "99", ResponseMessage = result.ErrorMessage });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaymentResponse
                {
                    ResponseCode = "99",
                    ResponseMessage = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        // ---------------------------- ICELL INTEGRATION ----------------------------

        /// <summary>
        /// Initiate New Subscription to i-Cell Aggregator
        /// </summary>
        [HttpPost("icell/subscribe")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> HandleNewSubscription([FromBody] ICellSubscriptionRequestDto request)
        {
            try
            {
                var icellUrl = _config["ICell:BaseUrl"]; // e.g. http://ip:port/SchedulingEngineWeb/services/CallSubscription
                var soapAction = _config["ICell:SoapAction:HandleNewSubscription"]; // optional

                var soapBody = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:sub=""http://subscriptionengine.ibm.com"">
   <soapenv:Header/>
   <soapenv:Body>
      <sub:handleNewSubscription>
         <custAttributesDTO>
            <cpId>{request.CpId}</cpId>
            <cpPwd>{request.CpPwd}</cpPwd>
            <msisdn>{request.Msisdn}</msisdn>
            <channelName>{request.ChannelName}</channelName>
            <productId>{request.ProductId}</productId>
            <cpName>{request.CpName}</cpName>
            <aocMsg1>{request.AocMsg1}</aocMsg1>
            <aocMsg2>{request.AocMsg2}</aocMsg2>
            <firstConfirmationDTTM>{request.FirstConfirmationDTTM:yyyy-MM-ddTHH:mm:ss.fffZ}</firstConfirmationDTTM>
         </custAttributesDTO>
      </sub:handleNewSubscription>
   </soapenv:Body>
</soapenv:Envelope>";

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, icellUrl)
                {
                    Content = new StringContent(soapBody, Encoding.UTF8, "text/xml")
                };
                // SOAP headers (optional depending on provider requirements)
                if (!string.IsNullOrWhiteSpace(soapAction))
                {
                    httpRequest.Headers.TryAddWithoutValidation("SOAPAction", soapAction);
                }
                httpRequest.Headers.TryAddWithoutValidation("Accept", "text/xml");

                var response = await _httpClient.SendAsync(httpRequest);
                var xmlResponse = await response.Content.ReadAsStringAsync();

                var parsed = ParseICellResponse(xmlResponse);
                return Ok(new { message = "New subscription sent successfully", response = parsed });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Unsubscribe / De-Subscription from i-Cell Aggregator
        /// </summary>
        [HttpPost("icell/unsubscribe")]
        //[Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> HandleDeSubscription([FromBody] ICellDeSubscriptionRequestDto request)
        {
            try
            {
                var icellUrl = _config["ICell:BaseUrl"];
                var soapAction = _config["ICell:SoapAction:HandleDeSubscription"]; // optional

                var soapBody = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
                  xmlns:sub=""http://subscriptionengine.ibm.com"">
   <soapenv:Header/>
   <soapenv:Body>
      <sub:handleDeSubscription>
         <custAttributesDTO>
            <msisdn>{request.Msisdn}</msisdn>
            <productId>{request.ProductId}</productId>
            <cpId>{request.CpId}</cpId>
            <cpPwd>{request.CpPwd}</cpPwd>
         </custAttributesDTO>
      </sub:handleDeSubscription>
   </soapenv:Body>
</soapenv:Envelope>";

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, icellUrl)
                {
                    Content = new StringContent(soapBody, Encoding.UTF8, "text/xml")
                };
                if (!string.IsNullOrWhiteSpace(soapAction))
                {
                    httpRequest.Headers.TryAddWithoutValidation("SOAPAction", soapAction);
                }
                httpRequest.Headers.TryAddWithoutValidation("Accept", "text/xml");

                var response = await _httpClient.SendAsync(httpRequest);
                var xmlResponse = await response.Content.ReadAsStringAsync();

                var parsed = ParseICellResponse(xmlResponse);
                return Ok(new { message = "De-subscription sent successfully", response = parsed });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// Receive DataSync Notification from i-Cell
        /// </summary>
        [HttpPost("icell/notification")]
        [AllowAnonymous]
        public async Task<IActionResult> ReceiveICellNotification([FromBody] string xmlNotification)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlNotification);

                var msisdn = xmlDoc.SelectSingleNode("//msisdn")?.InnerText;
                var productId = xmlDoc.SelectSingleNode("//productId")?.InnerText;
                var errorCode = xmlDoc.SelectSingleNode("//errorCode")?.InnerText;
                var errorMsg = xmlDoc.SelectSingleNode("//errorMsg")?.InnerText;

                await _subscriptionService.HandleICellDataSyncAsync(msisdn, productId, errorCode, errorMsg);

                // Return SOAP acknowledgment as per i-Cell spec
                var responseXml = @"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
   <soapenv:Body>
      <notificationToCPResponse xmlns=""http://SubscriptionEngine.ibm.com""/>
   </soapenv:Body>
</soapenv:Envelope>";

                return Content(responseXml, "text/xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // ---------------------------- UTILITIES ----------------------------
        private static object ParseICellResponse(string xml)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                return new
                {
                    errorCode = xmlDoc.SelectSingleNode("//errorCode")?.InnerText,
                    errorMsg = xmlDoc.SelectSingleNode("//errorMsg")?.InnerText,
                    msisdn = xmlDoc.SelectSingleNode("//msisdn")?.InnerText,
                    productId = xmlDoc.SelectSingleNode("//productId")?.InnerText,
                    transactionId = xmlDoc.SelectSingleNode("//temp2")?.InnerText,
                    chargingTime = xmlDoc.SelectSingleNode("//chargigTime")?.InnerText
                };
            }
            catch
            {
                return new { raw = xml };
            }
        }
    }

    // ---------------------------- DTOs ----------------------------
    public class ICellSubscriptionRequestDto
    {
        public string CpId { get; set; } // string to support large numeric IDs
        public string CpPwd { get; set; }
        public string Msisdn { get; set; }
        public string ChannelName { get; set; }
        public int ProductId { get; set; }
        public string CpName { get; set; }
        public int AocMsg1 { get; set; }
        public int AocMsg2 { get; set; }
        public DateTime FirstConfirmationDTTM { get; set; }
    }

    public class ICellDeSubscriptionRequestDto
    {
        public string Msisdn { get; set; }
        public int ProductId { get; set; }
        public string CpId { get; set; } // string to support large numeric IDs
        public string CpPwd { get; set; }
    }
}
