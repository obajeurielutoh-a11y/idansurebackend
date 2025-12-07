using System;
using System.Text.Json.Serialization;

namespace SubscriptionSystem.Application.DTOs
{
    public class PaystackWebhookDto
    {
        [JsonPropertyName("event")]
        public string Event { get; set; }

        [JsonPropertyName("data")]
        public PaystackTransactionData Data { get; set; }
    }

    public class PaystackTransactionData
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }


        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("transaction_date")]
        public DateTime TransactionDate { get; set; }

        [JsonPropertyName("customer")]
        public PaystackCustomer Customer { get; set; }

        [JsonPropertyName("metadata")]
        public PaystackMetadata Metadata { get; set; }
    }

    public class PaystackCustomer
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("customer_code")]
        public string CustomerCode { get; set; }

        [JsonPropertyName("first_name")]
        public string FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string LastName { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }
    }

    public class PaystackMetadata
    {
        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("plan_type")]
        public string PlanType { get; set; }
    }

    public class PaystackInitializeRequestDto
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("callback_url")]
        public string CallbackUrl { get; set; }

        [JsonPropertyName("reference")]
        public string? Reference { get; set; }

        [JsonPropertyName("metadata")]
        public PaystackMetadata Metadata { get; set; }
    }

    public class PaystackInitializeResponseDto
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public PaystackInitializeData Data { get; set; }
    }

    public class PaystackInitializeData
    {
        [JsonPropertyName("authorization_url")]
        public string AuthorizationUrl { get; set; }

        [JsonPropertyName("access_code")]
        public string AccessCode { get; set; }

        [JsonPropertyName("reference")]
        public string Reference { get; set; }
    }

    public class PaystackVerifyResponseDto
    {
        [JsonPropertyName("status")]
        public bool Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public PaystackTransactionData Data { get; set; }
    }
}