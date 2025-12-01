using System.Xml.Serialization;

namespace SubscriptionSystem.Application.DTOs
{
    [XmlRoot("root")]
    public class DeliveryNotificationDto
    {
        [XmlElement("shortcode")]
        public string Shortcode { get; set; }

        [XmlElement("correlator")]
        public string Correlator { get; set; }

        [XmlElement("msisdn")]
        public string Msisdn { get; set; }

        [XmlElement("deliverystatus")]
        public string DeliveryStatus { get; set; }

        [XmlElement("timestamp")]
        public string Timestamp { get; set; }
    }
}
