using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System.Text;

namespace SubscriptionSystem.Application.Services
{
    public class SmsService : ISmsService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<SmsService> _logger;

        public SmsService(IHttpClientFactory httpClientFactory, IConfiguration config, ILogger<SmsService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _logger = logger;
        }

        public async Task<(bool success, string errorMsg)> SendSmsAsync(string msisdn, string message)
        {
            try
            {
                var spId = _config["Sms:SpId"];
                var spPassword = _config["Sms:SpPassword"];
                var serviceId = _config["Sms:ServiceId"];
                var endpoint = _config["Sms:Endpoint"];
                var senderName = _config["Sms:SenderName"] ?? "77716"; // Default or config

                if (string.IsNullOrEmpty(spId) || string.IsNullOrEmpty(spPassword) || string.IsNullOrEmpty(endpoint))
                {
                    _logger.LogError("SMS configuration missing.");
                    return (false, "SMS configuration missing");
                }

                var timeStamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                // Correlator could be random or tracked
                var correlator = new Random().Next(1000, 9999).ToString();

                var soapEnvelope = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/send/v2_2/local"">
   <soapenv:Header>
      <v2:RequestSOAPHeader>
         <v2:spId>{spId}</v2:spId>
         <v2:spPassword>{spPassword}</v2:spPassword>
         <v2:serviceId>{serviceId}</v2:serviceId>
         <v2:timeStamp>{timeStamp}</v2:timeStamp>
      </v2:RequestSOAPHeader>
   </soapenv:Header>
   <soapenv:Body>
      <loc:sendSms>
         <loc:addresses>tel:{msisdn}</loc:addresses>
         <loc:senderName>{senderName}</loc:senderName>
         <loc:message>{System.Security.SecurityElement.Escape(message)}</loc:message>
         <correlator>{correlator}</correlator> 
      </loc:sendSms>
   </soapenv:Body>
</soapenv:Envelope>";

                var client = _httpClientFactory.CreateClient();
                var content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");
                
                // Add SOAPAction if required by the provider, often it's empty or specific
                // content.Headers.Add("SOAPAction", ""); 

                var response = await client.PostAsync(endpoint, content);
                var responseString = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                     // Basic check for success in SOAP response - adjust based on actual success response format
                     // Often looking for a specific result code or lack of Fault
                    if (responseString.Contains("Fault", StringComparison.OrdinalIgnoreCase))
                    {
                         _logger.LogError("SMS SOAP Fault: {Response}", responseString);
                         return (false, "SMS Provider Error");
                    }
                    return (true, string.Empty);
                }
                else
                {
                    _logger.LogError("SMS HTTP Error: {StatusCode} {Response}", response.StatusCode, responseString);
                    return (false, $"HTTP {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending SMS to {Msisdn}", msisdn);
                return (false, "Internal Error");
            }
        }
    }
}
