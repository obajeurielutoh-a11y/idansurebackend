using SubscriptionSystem.Application.DTOs;

internal class SubscriptionPurchaseRequest : SubscriptionRequestDto
{
    public string TransactionId { get; set; }
    public string Email { get; set; }
    public int AmountPaid { get; set; }
    public string Currency { get; set; }
    public string PhoneNumber { get; set; }
    public object Plan { get; set; }
}