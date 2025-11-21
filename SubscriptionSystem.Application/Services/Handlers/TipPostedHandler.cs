using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Events;

namespace SubscriptionSystem.Application.Services.Handlers
{
    public class TipPostedHandler : IDomainEventHandler<TipPostedEvent>
    {
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly IWhatsAppProvider _whatsAppProvider;
        private readonly ILogger<TipPostedHandler> _logger;

        public TipPostedHandler(ISubscriptionRepository subscriptionRepository, IUserRepository userRepository, IWhatsAppProvider whatsAppProvider, ILogger<TipPostedHandler> logger)
        {
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _whatsAppProvider = whatsAppProvider;
            _logger = logger;
        }

        public async Task HandleAsync(TipPostedEvent evt, CancellationToken cancellationToken = default)
        {
            // naive iteration over active subscribers; consider batching/paging
            var activeSubs = await _subscriptionRepository.GetAllAsync();
            var active = activeSubs.Where(s => s.IsActive && s.ExpiryDate > DateTime.UtcNow).ToList();

            foreach (var sub in active)
            {
                var user = await _userRepository.GetUserByIdAsync(sub.UserId);
                if (user?.PhoneNumber == null) continue;
                var body = $"New tip posted: {evt.Tournament} | {evt.Team1} vs {evt.Team2} on {evt.MatchDate:dd MMM}. Check app for details.";
                await _whatsAppProvider.SendMessageAsync(user.PhoneNumber, body, cancellationToken);
            }
        }
    }
}
