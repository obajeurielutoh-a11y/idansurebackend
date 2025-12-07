using System.Xml.Serialization;

namespace SubscriptionSystem.Application.DTOs
{
    [XmlRoot("root")]
    public class UssdNotificationDto
    {
        [XmlElement("shortcode")]
        public string Shortcode { get; set; }

        [XmlElement("msisdn")]
        public string Msisdn { get; set; }

        [XmlElement("choice")]
        public string Choice { get; set; }

        [XmlElement("timestamp")]
        public string Timestamp { get; set; }

        [XmlElement("ussdid")]
        public string UssdId { get; set; }

        [XmlElement("status")]
        public string Status { get; set; }

        [XmlElement("linkid")]
        public string LinkId { get; set; }
    }
}
