public class CredoWebhookPayloadDto
{
    // Use lowercase to match the JSON property name
    public string @event { get; set; }
    public CredoWebhookData data { get; set; }
}

public class CredoWebhookData
{
    public string transRef { get; set; }
    public string reference { get; set; }
    public decimal amount { get; set; }
    public string crn { get; set; }
    public string currency { get; set; }
    public CredoCustomerDto customer { get; set; }
    public string customerId { get; set; }
    public string narration { get; set; }
    public string paymentMethod { get; set; }
    public decimal settlementAmount { get; set; }
    public int status { get; set; }
    public decimal transAmount { get; set; }
    public decimal transFeeAmount { get; set; }
    public DateTime transactionDate { get; set; }
    // Add any missing properties from your JSON
    public string metadata { get; set; }
}

public class CredoCustomerDto
{
    public string customerEmail { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string phoneNo { get; set; }
}