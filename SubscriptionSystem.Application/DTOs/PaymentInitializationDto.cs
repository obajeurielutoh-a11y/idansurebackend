
namespace SubscriptionSystem.Application.DTOs
{

    //public class PaymentInitializationDto
    //{
    //    public int Amount { get; set; }
    //    public int Bearer { get; set; }
    //    public string CallbackUrl { get; set; }
    //    public string Currency { get; set; }
    //    public string CustomerFirstName { get; set; }
    //    public string CustomerLastName { get; set; }
    //    public string CustomerPhoneNumber { get; set; }
    //    public string Email { get; set; }
    //    public string PlanType { get; set; }
    //}
    public class PaymentInitializationDto
    {
        public decimal Amount { get; set; }
        public int Bearer { get; set; }
        public string CallbackUrl { get; set; }
        public string Currency { get; set; } = "NGN";
        public string CustomerFirstName { get; set; }
        public string CustomerLastName { get; set; }
        public string CustomerPhoneNumber { get; set; }
        public string Email { get; set; }
        public string Reference { get; set; } // Optional
        public string BankAccount { get; set; } // For metadata
        public List<CustomFieldDto> CustomFields { get; set; } // For metadata
    }

    public class CustomFieldDto
    {
        public string VariableName { get; set; }
        public string Value { get; set; }
        public string DisplayName { get; set; }
    }
}
