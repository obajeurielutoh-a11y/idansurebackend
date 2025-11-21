using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Events;
using SubscriptionSystem.Application.Interfaces;

namespace SubscriptionSystem.Application.Services.Handlers
{
    public class SubscriptionActivatedHandler : IDomainEventHandler<SubscriptionActivatedEvent>
    {
        private readonly IWhatsAppProvider _whatsAppProvider;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<SubscriptionActivatedHandler> _logger;

        public SubscriptionActivatedHandler(IWhatsAppProvider whatsAppProvider, IUserRepository userRepository, ILogger<SubscriptionActivatedHandler> logger)
        {
            _whatsAppProvider = whatsAppProvider;
            _userRepository = userRepository;
            _logger = logger;
        }

        public async Task HandleAsync(SubscriptionActivatedEvent evt, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetUserByIdAsync(evt.UserId);
            if (user?.PhoneNumber == null)
            {
                _logger.LogWarning("User {UserId} missing phone number for WhatsApp notification", evt.UserId);
                return;
            }
            var msg = $"✅ Subscription Activated: Plan {evt.Plan} valid till {evt.ExpiryDate:dd MMM}. Thanks for supporting IdanSure!";
            await _whatsAppProvider.SendMessageAsync(user.PhoneNumber, msg, cancellationToken);
        }
    }
}
