using System.Xml.Serialization;

namespace SubscriptionSystem.Application.DTOs
{
    [XmlRoot("root")]
    public class MoNotificationDto
    {
        [XmlElement("shortcode")]
        public string Shortcode { get; set; } = string.Empty;

        [XmlElement("msisdn")]
        public string Msisdn { get; set; } = string.Empty;

        [XmlElement("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp from external system (if provided). Server-side processing auto-generates its own timestamp.
        /// </summary>
        [XmlElement("timestamp")]
        public string? Timestamp { get; set; }
    }
}
