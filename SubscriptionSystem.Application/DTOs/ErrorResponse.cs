namespace SubscriptionSystem.Application.DTOs
{
    public class ErrorResponse
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public double ExecTime { get; set; }
        public List<string> Error { get; set; }
    }
}
