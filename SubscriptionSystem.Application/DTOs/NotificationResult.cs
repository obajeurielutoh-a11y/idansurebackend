namespace SubscriptionSystem.Application.DTOs
{
    public class NotificationResult<T>
    {
        public bool IsSuccess { get; set; }
        public string ResponseMessage { get; set; }
        public T Data { get; set; }
        public string Message { get; internal set; }
    }
}

