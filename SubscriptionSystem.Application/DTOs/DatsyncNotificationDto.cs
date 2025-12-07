using System.Xml.Serialization;

namespace SubscriptionSystem.Application.DTOs
{
    // This DTO maps to the inner notificationRespDTO inside the SOAP body
    [XmlRoot("notificationRespDTO")]
    public class DatsyncNotificationDto
    {
        [XmlElement("amount")]
        public string Amount { get; set; }

        [XmlElement("chargigTime")]
        public string ChargingTime { get; set; }

        [XmlElement("errorCode")]
        public string ErrorCode { get; set; }

        [XmlElement("errorMsg")]
        public string ErrorMsg { get; set; }

        [XmlElement("lowBalance")]
        public string LowBalance { get; set; }

        [XmlElement("msisdn")]
        public string Msisdn { get; set; }

        [XmlElement("productId")]
        public string ProductId { get; set; }

        [XmlElement("xactionId")]
        public string TransactionId { get; set; }
    }
}
