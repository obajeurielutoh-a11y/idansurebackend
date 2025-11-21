using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.Interfaces;
using System.Text;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/ussd/airtel")]
    public class UssdController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IAsedeyhotPredictionService _asedeyhotPredictionService;
        private readonly ILogger<UssdController> _logger;
        private readonly IConfiguration _config;

        public UssdController(
            IHttpClientFactory httpClientFactory,
            IAsedeyhotPredictionService asedeyhotPredictionService,
            ILogger<UssdController> logger,
            IConfiguration config)
        {
            _httpClientFactory = httpClientFactory;
            _asedeyhotPredictionService = asedeyhotPredictionService;
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        [Consumes("application/x-www-form-urlencoded", "application/json")]
        [Produces("text/plain")]
        public async Task<IActionResult> Handle([FromForm] UssdRequest? form, [FromBody] UssdRequest? json)
        {
            var req = form ?? json ?? new UssdRequest();

            var sessionId = req.SessionId ?? Request.Query["sessionId"].ToString();
            var msisdn = req.Msisdn ?? req.PhoneNumber ?? Request.Query["msisdn"].ToString();
            var text = req.Text ?? Request.Query["text"].ToString();

            if (string.IsNullOrWhiteSpace(sessionId)) sessionId = Guid.NewGuid().ToString("N");

            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    var menu = new StringBuilder();
                    menu.AppendLine("CON Welcome to IdanSure Predictions");
                    menu.AppendLine("1. Get today prediction");
                    menu.Append("2. Subscribe (Airtime)");
                    return Content(menu.ToString(), "text/plain; charset=utf-8");
                }

                var parts = text.Split('*', StringSplitOptions.RemoveEmptyEntries);
                var choice = parts.LastOrDefault() ?? text.Trim();

                switch (choice)
                {
                    case "1":
                        {
                            var result = await _asedeyhotPredictionService.GetPredictionsAsync(1, 1);
                            if (!result.IsSuccess || result.Data.Items.Count == 0)
                                return Content("END No predictions available now. Please try later.", "text/plain; charset=utf-8");

                            var p = result.Data.Items.First();
                            var response = Truncate($"END Today: {p.AlphanumericPrediction}\n{p.NonAlphanumericDetails}", 160);
                            return Content(response, "text/plain; charset=utf-8");
                        }
                    case "2":
                        {
                            var subscribeOk = await TryIcellSubscribeAsync(msisdn);
                            if (!subscribeOk.success)
                            {
                                var msg = !string.IsNullOrWhiteSpace(subscribeOk.errorMsg)
                                    ? subscribeOk.errorMsg
                                    : "Subscription failed. Please try again later.";
                                return Content($"END {msg}", "text/plain; charset=utf-8");
                            }

                            var result = await _asedeyhotPredictionService.GetPredictionsAsync(1, 1);
                            if (!result.IsSuccess || result.Data.Items.Count == 0)
                                return Content("END Subscribed. No predictions available right now.", "text/plain; charset=utf-8");

                            var p = result.Data.Items.First();
                            var response = Truncate($"END Subscribed successfully.\n{p.AlphanumericPrediction}\n{p.NonAlphanumericDetails}", 160);
                            return Content(response, "text/plain; charset=utf-8");
                        }
                    default:
                        return Content("END Invalid option.", "text/plain; charset=utf-8");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "USSD processing error. sessionId={SessionId} msisdn={Msisdn}", sessionId, msisdn);
                return Content("END Service error. Try again later.", "text/plain; charset=utf-8");
            }
        }

        private async Task<(bool success, string errorMsg)> TryIcellSubscribeAsync(string? msisdn)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(msisdn))
                    return (false, "Missing MSISDN");

                var cpId = _config.GetValue<string>("ICell:CpId"); // cpId as string to match provider sample
                var cpPwd = _config["ICell:CpPwd"];
                var cpName = _config["ICell:CpName"] ?? string.Empty;
                var productId = _config.GetValue<int>("ICell:ProductId");
                var baseUrl = _config["ICell:BaseUrl"]; // http://IP:PORT/SchedulingEngineWeb/services/CallSubscription
                var soapAction = _config["ICell:SoapAction:HandleNewSubscription"]; // optional
                if (string.IsNullOrWhiteSpace(baseUrl))
                    return (false, "iCell endpoint not configured");

                var channelName = "USSD";
                var aoc1 = 1;
                var aoc2 = 1;
                var firstDttm = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");

                var soapBody = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                    "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:sub=\"http://subscriptionengine.ibm.com\">" +
                    "<soapenv:Header/>" +
                    "<soapenv:Body>" +
                    "<sub:handleNewSubscription>" +
                    "<custAttributesDTO>" +
                    $"<cpId>{System.Security.SecurityElement.Escape(cpId)}</cpId>" +
                    $"<cpPwd>{System.Security.SecurityElement.Escape(cpPwd)}</cpPwd>" +
                    $"<msisdn>{System.Security.SecurityElement.Escape(msisdn)}</msisdn>" +
                    $"<channelName>{System.Security.SecurityElement.Escape(channelName)}</channelName>" +
                    $"<productId>{productId}</productId>" +
                    $"<cpName>{System.Security.SecurityElement.Escape(cpName)}</cpName>" +
                    $"<aocMsg1>{aoc1}</aocMsg1>" +
                    $"<aocMsg2>{aoc2}</aocMsg2>" +
                    $"<firstConfirmationDTTM>{firstDttm}</firstConfirmationDTTM>" +
                    "</custAttributesDTO>" +
                    "</sub:handleNewSubscription>" +
                    "</soapenv:Body>" +
                    "</soapenv:Envelope>";

                var client = _httpClientFactory.CreateClient();
                using var request = new HttpRequestMessage(HttpMethod.Post, baseUrl)
                {
                    Content = new StringContent(soapBody, Encoding.UTF8, "text/xml")
                };
                if (!string.IsNullOrWhiteSpace(soapAction))
                {
                    request.Headers.TryAddWithoutValidation("SOAPAction", soapAction);
                }
                request.Headers.TryAddWithoutValidation("Accept", "text/xml");

                var resp = await client.SendAsync(request);
                var xml = await resp.Content.ReadAsStringAsync();

                var success = xml.Contains("<errorCode>1000</errorCode>", StringComparison.OrdinalIgnoreCase);
                if (!success)
                {
                    var start = xml.IndexOf("<errorMsg>", StringComparison.OrdinalIgnoreCase);
                    var end = xml.IndexOf("</errorMsg>", StringComparison.OrdinalIgnoreCase);
                    var msg = (start >= 0 && end > start) ? xml.Substring(start + 10, end - (start + 10)) : "Subscription failed";
                    return (false, msg);
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling iCell subscription");
                return (false, "Subscription error");
            }
        }

        private static string Truncate(string input, int max)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Length <= max ? input : input.Substring(0, max);
        }
    }

    public class UssdRequest
    {
        public string? SessionId { get; set; }
        public string? ServiceCode { get; set; }
        public string? Msisdn { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Network { get; set; }
        public string? Text { get; set; }
    }
}
