using System.Text.Json.Serialization;

namespace SubscriptionSystem.Application.DTOs
{
    public class WebhookPayloadDto
    {
        public string Event { get; set; }
        public WebhookDataDto Data { get; set; }
     
        
    }
   
    public class WebhookDataDto
    {
        public string TransRef { get; set; }
        public string Reference { get; set; }
        public decimal Amount { get; set; }
        public decimal TransAmount { get; set; }
        public decimal TransFeeAmount { get; set; }
        public decimal SettlementAmount { get; set; }
        public string CustomerId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Currency { get; set; }
        public int Status { get; set; }
        public string Crn { get; set; }
        public string PaymentMethod { get; set; }
        public string Narration { get; set; }
        public CustomerDto Customer { get; set; }
       
    }

    public class CustomerDto
    {
        public string CustomerEmail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNo { get; set; }
    }

   
}