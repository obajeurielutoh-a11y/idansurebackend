namespace SubscriptionSystem.Application.DTOs
{
    public class ServiceResult<T>
    {
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; }
        public T Data { get; set; }
        public string Message { get; internal set; }
    }
}

