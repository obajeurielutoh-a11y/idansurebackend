namespace SubscriptionSystem.Application.DTOs
{
    public class DirectCardChargeDto
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Reference { get; set; }
        public CardDetails Card { get; set; }
        public CustomerDetails Customer { get; set; }
        public string CallbackUrl { get; set; }
        public bool Preauthorize { get; set; }
        public AuthorizationDetails Authorization { get; set; }
    }
}
public class CustomerDetails
{
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class AuthorizationDetails
{
    public string Mode { get; set; }
    public string Pin { get; set; }
}

public class CardDetails
{
    public string Pan { get; set; }
    public string Cvv { get; set; }
    public int ExpiryYear { get; set; }
    public int ExpiryMonth { get; set; }
}