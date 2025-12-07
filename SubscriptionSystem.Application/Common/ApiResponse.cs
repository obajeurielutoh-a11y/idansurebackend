namespace SubscriptionSystem.Application.Common
{
    public class ApiResponse<T>
    {
        public int Status { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public double ExecTime { get; set; }
        public List<string> Error { get; set; }
    }
}
