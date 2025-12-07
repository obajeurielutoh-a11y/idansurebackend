using System.Text.Json.Serialization;

namespace SubscriptionSystem.Domain.Entities
{
    public class PaymentResponse
    {
        [JsonIgnore]
        public string Id { get; set; } // This property will be ignored during serialization

        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
    }
}
