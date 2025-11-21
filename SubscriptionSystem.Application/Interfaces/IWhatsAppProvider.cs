using System.Threading.Tasks;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface IWhatsAppProvider
    {
        Task SendMessageAsync(string toPhoneE164, string message, CancellationToken cancellationToken = default);
    }
}
