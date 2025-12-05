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
        private readonly ILogger<SubscriptionController> _logger;
        private readonly IPredictionPostService _predictionPostService;

        public SubscriptionController(
            ISubscriptionService subscriptionService,
            IPaymentService paymentService,
            IConfiguration config,
            IHttpClientFactory httpClientFactory,
            ILogger<SubscriptionController> logger,
            IPredictionPostService predictionPostService)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _config = config;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _predictionPostService = predictionPostService ?? throw new ArgumentNullException(nameof(predictionPostService));
        }

        // ---------------------------- NEW: MO webhook & Admin publish ----------------------------

        // Simple in-memory keyword -> product mapping. Override or move to configuration if needed.
        private static readonly System.Collections.Generic.Dictionary<string, (int productId, string channelName)> KeywordMap =
            new()
            {
                // Only daily subscription keyword supported
                { "idsd", (productId: 1, channelName: "DAILY") }
            };

        /// <summary>
        /// Mobile-Originated webhook from aggregator (MO callback). Accepts form or JSON fields.
        /// Expected fields (common): msisdn, keyword, shortcode, message
        /// When keyword maps to a product, trigger i-Cell subscription on behalf of that msisdn.
        /// </summary>
        [HttpPost("mo/webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleMoWebhook([FromForm] MoCallbackDto formDto)
        {
            try
            {
                // Support JSON body fallback
                MoCallbackDto? dto = formDto;
                if (dto == null || string.IsNullOrWhiteSpace(dto.Keyword))
                {
                    // try to read body as JSON
                    try
                    {
                        Request.Body.Position = 0;
                    }
                    catch { }
                    using var sr = new System.IO.StreamReader(Request.Body);
                    var body = await sr.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        try { dto = System.Text.Json.JsonSerializer.Deserialize<MoCallbackDto>(body); } catch { }
                    }
                }

                if (dto == null || string.IsNullOrWhiteSpace(dto.Msisdn) || string.IsNullOrWhiteSpace(dto.Keyword))
                    return BadRequest(new { message = "Missing msisdn or keyword" });

                var kw = dto.Keyword.Trim().ToLowerInvariant();
                if (!KeywordMap.TryGetValue(kw, out var mapping))
                    return Ok(new { message = "Keyword not recognised; ignored" });

                // Build subscription request using defaults from config when present
                var cpId = _config["ICell:DefaultCpId"] ?? _config["ICell:CpId"] ?? string.Empty;
                var cpPwd = _config["ICell:DefaultCpPwd"] ?? _config["ICell:CpPwd"] ?? string.Empty;

                var req = new ICellSubscriptionRequestDto
                {
                    CpId = cpId,
                    CpPwd = cpPwd,
                    Msisdn = dto.Msisdn,
                    ChannelName = mapping.channelName,
                    ProductId = mapping.productId,
                    CpName = _config["ICell:DefaultCpName"] ?? "",
                    AocMsg1 = 0,
                    AocMsg2 = 0,
                    FirstConfirmationDTTM = DateTime.UtcNow
                };

                var result = await SendICellSubscriptionAsync(req);
                return Ok(new { message = "MO processed", result });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// MO Notification endpoint from Service Provider in XML format.
        /// Accepts XML with shortcode, msisdn, and message. Timestamp is auto-generated server-side.
        /// Example XML: &lt;root&gt;&lt;shortcode&gt;77716&lt;/shortcode&gt;&lt;msisdn&gt;+2349094048672&lt;/msisdn&gt;&lt;message&gt;742&lt;/message&gt;&lt;/root&gt;
        /// </summary>
        [HttpPost("mo")]
        [Consumes("application/xml", "text/xml")]
        [AllowAnonymous]
        public async Task<IActionResult> ReceiveMoNotification([FromBody] MoNotificationDto notification)
        {
            try
            {
                if (notification == null)
                    return BadRequest(new { message = "Invalid XML payload." });

                // Auto-generate timestamp server-side (ignore any timestamp from request)
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                _logger.LogInformation(
                    "Received MO Notification: Shortcode={Shortcode}, Msisdn={Msisdn}, Message={Message}, Timestamp={Timestamp}",
                    notification.Shortcode, notification.Msisdn, notification.Message, timestamp);

                // Process the MO notification: parse message for keywords and trigger subscription
                var keyword = notification.Message?.Trim().ToLowerInvariant() ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    _logger.LogWarning("MO notification received with empty message from {Msisdn}", notification.Msisdn);
                    return Ok(new { message = "Notification received but message was empty.", timestamp });
                }

                // Check if keyword maps to a product
                if (KeywordMap.TryGetValue(keyword, out var mapping))
                {
                    _logger.LogInformation("Keyword '{Keyword}' matched product {ProductId} for msisdn {Msisdn}", 
                        keyword, mapping.productId, notification.Msisdn);

                    try
                    {
                        // Build subscription request using defaults from config
                        var cpId = _config["ICell:DefaultCpId"] ?? _config["ICell:CpId"] ?? string.Empty;
                        var cpPwd = _config["ICell:DefaultCpPwd"] ?? _config["ICell:CpPwd"] ?? string.Empty;

                        var subscriptionRequest = new ICellSubscriptionRequestDto
                        {
                            CpId = cpId,
                            CpPwd = cpPwd,
                            Msisdn = notification.Msisdn,
                            ChannelName = mapping.channelName,
                            ProductId = mapping.productId,
                            CpName = _config["ICell:DefaultCpName"] ?? string.Empty,
                            AocMsg1 = 0,
                            AocMsg2 = 0,
                            FirstConfirmationDTTM = DateTime.UtcNow
                        };

                        // Trigger subscription to i-Cell aggregator
                        var subscriptionResult = await SendICellSubscriptionAsync(subscriptionRequest);
                        
                        _logger.LogInformation("Subscription triggered for {Msisdn} with keyword '{Keyword}'. Result: {@Result}",
                            notification.Msisdn, keyword, subscriptionResult);

                        // Check if user has already received today's prediction (one per day security)
                        var today = DateTime.UtcNow.Date;
                        var hasReceivedToday = await _predictionPostService.HasReceivedTodaysPredictionAsync(notification.Msisdn, today);

                        string predictionMessage = string.Empty;
                        if (!hasReceivedToday)
                        {
                            // Get today's prediction
                            var todaysPrediction = await _predictionPostService.GetPredictionForDateAsync(today);

                            if (todaysPrediction != null)
                            {
                                // Format prediction message (max 500 chars)
                                predictionMessage = $"Today's Tip: {todaysPrediction.Team1} vs {todaysPrediction.Team2} - {todaysPrediction.PredictionOutcome}";

                                // Record that this msisdn received today's prediction
                                await _predictionPostService.RecordPredictionSentAsync(notification.Msisdn, todaysPrediction.Id, today);

                                _logger.LogInformation("Sent today's prediction to {Msisdn}. Prediction ID: {PredictionId}", 
                                    notification.Msisdn, todaysPrediction.Id);
                            }
                            else
                            {
                                _logger.LogWarning("No prediction available for today to send to {Msisdn}", notification.Msisdn);
                                predictionMessage = "Welcome to daily tips! No prediction available yet for today.";
                            }
                        }
                        else
                        {
                            _logger.LogInformation("User {Msisdn} already received today's prediction. Skipping.", notification.Msisdn);
                            predictionMessage = "You have already received today's prediction.";
                        }

                        return Ok(new 
                        { 
                            message = "Notification processed and subscription triggered successfully.", 
                            timestamp,
                            keyword,
                            msisdn = notification.Msisdn,
                            productId = mapping.productId,
                            subscriptionResult,
                            predictionSent = !hasReceivedToday && !string.IsNullOrEmpty(predictionMessage),
                            predictionMessage
                        });
                    }
                    catch (Exception subEx)
                    {
                        _logger.LogError(subEx, "Failed to trigger subscription for {Msisdn} with keyword '{Keyword}'", 
                            notification.Msisdn, keyword);
                        
                        return Ok(new 
                        { 
                            message = "Notification received but subscription failed.", 
                            timestamp,
                            error = subEx.Message 
                        });
                    }
                }
                else
                {
                    _logger.LogInformation("Keyword '{Keyword}' not recognized from {Msisdn}. No action taken.", 
                        keyword, notification.Msisdn);
                    
                    return Ok(new 
                    { 
                        message = "Notification received but keyword not recognized.", 
                        timestamp,
                        keyword 
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MO notification");
                return StatusCode(500, new { message = "An error occurred processing the notification.", detail = ex.Message });
            }
        }

        /// <summary>
        /// Admin publish endpoint: accepts up to 150 characters and triggers subscription calls for provided msisdns.
        /// Authentication: either authenticated user in role 'Admin' OR header `X-Admin-ApiKey` matching config `Subscription:AdminApiKey`.
        /// </summary>
        [HttpPost("admin/publish")]
        public async Task<IActionResult> AdminPublish([FromBody] AdminPublishDto request)
        {
            try
            {
                // Auth check: role-based or API key
                var apiKey = Request.Headers.ContainsKey("X-Admin-ApiKey") ? Request.Headers["X-Admin-ApiKey"].ToString() : string.Empty;
                var configuredKey = _config["Subscription:AdminApiKey"] ?? string.Empty;
                if (!(User?.IsInRole("Admin") == true || (!string.IsNullOrWhiteSpace(configuredKey) && apiKey == configuredKey)))
                    return Unauthorized(new { message = "Admin authentication required" });

                if (request == null || string.IsNullOrWhiteSpace(request.Message))
                    return BadRequest(new { message = "message is required" });

                if (request.Message.Length > 150)
                    return BadRequest(new { message = "message must be 150 characters or fewer" });

                if (request.Msisdns == null || request.Msisdns.Length == 0)
                    return BadRequest(new { message = "Provide at least one msisdn to target" });

                // Determine product mapping
                if (string.IsNullOrWhiteSpace(request.Keyword))
                    return BadRequest(new { message = "keyword is required to determine subscription product" });

                var kw = request.Keyword.Trim().ToLowerInvariant();
                if (!KeywordMap.TryGetValue(kw, out var mapping))
                    return BadRequest(new { message = "unknown keyword" });

                var cpId = _config["ICell:DefaultCpId"] ?? _config["ICell:CpId"] ?? string.Empty;
                var cpPwd = _config["ICell:DefaultCpPwd"] ?? _config["ICell:CpPwd"] ?? string.Empty;

                var results = new System.Collections.Generic.List<object>();
                foreach (var msisdn in request.Msisdns)
                {
                    var req = new ICellSubscriptionRequestDto
                    {
                        CpId = cpId,
                        CpPwd = cpPwd,
                        Msisdn = msisdn,
                        ChannelName = mapping.channelName,
                        ProductId = mapping.productId,
                        CpName = _config["ICell:DefaultCpName"] ?? string.Empty,
                        AocMsg1 = 0,
                        AocMsg2 = 0,
                        FirstConfirmationDTTM = DateTime.UtcNow
                    };

                    var r = await SendICellSubscriptionAsync(req);
                    results.Add(new { msisdn, r });
                }

                return Ok(new { message = "Published and subscription attempts sent", results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
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

                if (string.IsNullOrWhiteSpace(icellUrl))
                {
                    return BadRequest(new { message = "ICell:BaseUrl is not configured. Set configuration key 'ICell:BaseUrl' to the aggregator endpoint (e.g. 'http://host:3991/SchedulingEngineWeb/services/CallSubscription')." });
                }

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

                // Dry-run header support so you can inspect the generated SOAP from Swagger/UI
                if (Request?.Headers != null && Request.Headers.ContainsKey("X-Dry-Run") && string.Equals(Request.Headers["X-Dry-Run"].ToString(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return Ok(new { dryRun = true, soap = soapBody });
                }

                if (!Uri.TryCreate(icellUrl, UriKind.Absolute, out var _))
                {
                    return BadRequest(new { message = "ICell:BaseUrl must be an absolute URI (e.g. 'http://host:3991/SchedulingEngineWeb/services/CallSubscription')." });
                }

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

                try
                {
                    var response = await _httpClient.SendAsync(httpRequest);
                    var xmlResponse = await response.Content.ReadAsStringAsync();
                    var parsed = ParseICellResponse(xmlResponse);
                    return Ok(new { message = "New subscription sent successfully", response = parsed });
                }
                catch (HttpRequestException hre)
                {
                    return StatusCode(502, new { message = "Failed to contact i-Cell endpoint", detail = hre.Message });
                }
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
                var parsed = await SendICellDeSubscriptionAsync(request);
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
                // Validate incoming XML — return 400 if malformed to avoid 500s on bad payloads
                var xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.LoadXml(xmlNotification);
                }
                catch (XmlException xe)
                {
                    _logger.LogWarning(xe, "Invalid XML received in ICell notification");
                    return BadRequest(new { message = "Invalid XML payload", detail = xe.Message });
                }

                // Namespace-aware lookup: support both namespaced and non-namespaced payloads
                var nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("soapenv", "http://schemas.xmlsoap.org/soap/envelope/");
                nsmgr.AddNamespace("p", "http://SubscriptionEngine.ibm.com");

                XmlNode? notificationNode = xmlDoc.SelectSingleNode("//notificationRespDTO")
                                            ?? xmlDoc.SelectSingleNode("//p:notificationRespDTO", nsmgr)
                                            ?? xmlDoc.SelectSingleNode("//soapenv:Body//notificationRespDTO", nsmgr);

                if (notificationNode == null)
                {
                    _logger.LogWarning("ICell notification missing expected 'notificationRespDTO' node.");
                    return BadRequest(new { message = "Missing notificationRespDTO node in SOAP payload" });
                }

                var msisdn = notificationNode.SelectSingleNode(".//msisdn")?.InnerText;
                var productId = notificationNode.SelectSingleNode(".//productId")?.InnerText;
                var errorCode = notificationNode.SelectSingleNode(".//errorCode")?.InnerText;
                var errorMsg = notificationNode.SelectSingleNode(".//errorMsg")?.InnerText;

                await _subscriptionService.HandleICellDataSyncAsync(msisdn ?? string.Empty, productId, errorCode, errorMsg);

                // Return SOAP acknowledgment as per i-Cell spec
                var responseXml = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\">\n   <soapenv:Body>\n      <notificationToCPResponse xmlns=\"http://SubscriptionEngine.ibm.com\"/>\n   </soapenv:Body>\n</soapenv:Envelope>";

                return Content(responseXml, "text/xml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ICell notification");
                return StatusCode(500, new { message = "An error occurred processing the notification.", detail = ex.Message });
            }
        }

        // ---------------------------- UTILITIES ----------------------------
          private async Task<object> SendICellSubscriptionAsync(ICellSubscriptionRequestDto request)
          {
                var icellUrl = _config["ICell:BaseUrl"];
                var soapAction = _config["ICell:SoapAction:HandleNewSubscription"];

                if (string.IsNullOrWhiteSpace(icellUrl))
                {
                    throw new InvalidOperationException("ICell:BaseUrl is not configured. Set configuration key 'ICell:BaseUrl' to the aggregator endpoint.");
                }

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
                if (!string.IsNullOrWhiteSpace(soapAction))
                {
                     httpRequest.Headers.TryAddWithoutValidation("SOAPAction", soapAction);
                }
                httpRequest.Headers.TryAddWithoutValidation("Accept", "text/xml");

                var response = await _httpClient.SendAsync(httpRequest);
                var xmlResponse = await response.Content.ReadAsStringAsync();
                return ParseICellResponse(xmlResponse);
          }

          private async Task<object> SendICellDeSubscriptionAsync(ICellDeSubscriptionRequestDto request)
          {
                var icellUrl = _config["ICell:BaseUrl"];
                var soapAction = _config["ICell:SoapAction:HandleDeSubscription"];

                if (string.IsNullOrWhiteSpace(icellUrl))
                {
                    throw new InvalidOperationException("ICell:BaseUrl is not configured. Set configuration key 'ICell:BaseUrl' to the aggregator endpoint.");
                }

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
                return ParseICellResponse(xmlResponse);
          }
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
        public string CpId { get; set; } = string.Empty; // string to support large numeric IDs
        public string CpPwd { get; set; } = string.Empty;
        public string Msisdn { get; set; } = string.Empty;
        public string ChannelName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string CpName { get; set; } = string.Empty;
        public int AocMsg1 { get; set; }
        public int AocMsg2 { get; set; }
        public DateTime FirstConfirmationDTTM { get; set; }
    }

    public class ICellDeSubscriptionRequestDto
    {
        public string Msisdn { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string CpId { get; set; } = string.Empty; // string to support large numeric IDs
        public string CpPwd { get; set; } = string.Empty;
    }
    
    public class MoCallbackDto
    {
        public string? Msisdn { get; set; }
        public string? Keyword { get; set; }
        public string? Shortcode { get; set; }
        public string? Message { get; set; }
        public string? Timestamp { get; set; }
    }

    public class AdminPublishDto
    {
        public string? Message { get; set; }
        public string[]? Msisdns { get; set; }
        public string? Keyword { get; set; }
        public string? Language { get; set; }
    }
}
