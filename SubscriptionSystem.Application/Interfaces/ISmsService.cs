namespace SubscriptionSystem.Application.Interfaces
{
    public interface ISmsService
    {
        Task<(bool success, string errorMsg)> SendSmsAsync(string msisdn, string message);
    }
}
