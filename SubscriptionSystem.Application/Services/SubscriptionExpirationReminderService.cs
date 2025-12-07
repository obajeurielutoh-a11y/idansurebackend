//using Microsoft.Extensions.Logging;
//using SubscriptionSystem.Application.Interfaces;
//namespace SubscriptionSystem.Application.Services
//{
//    public class SubscriptionExpirationReminderService : IHostedService, IDisposable
//    {
//        private readonly ISubscriptionRepository _subscriptionRepository;
//        private readonly IEmailService _emailService;
//        private readonly ILogger<SubscriptionExpirationReminderService> _logger;
//        private Timer _timer;

//        public SubscriptionExpirationReminderService(
//            ISubscriptionRepository subscriptionRepository,
//            IEmailService emailService,
//            ILogger<SubscriptionExpirationReminderService> logger)
//        {
//            _subscriptionRepository = subscriptionRepository;
//            _emailService = emailService;
//            _logger = logger;
//        }

//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Subscription Expiration Reminder Service started.");

//            // Run the reminder check every 24 hours
//            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(24));

//            return Task.CompletedTask;
//        }

//        private async void DoWork(object state)
//        {
//            _logger.LogInformation("Checking for expiring subscriptions...");

//            try
//            {
//                // Get subscriptions expiring in the next 3 days
//                var expiringSubscriptions = await _subscriptionRepository.GetSubscriptionsExpiringSoonAsync(DateTime.UtcNow.AddDays(3));

//                foreach (var subscription in expiringSubscriptions)
//                {
//                    _logger.LogInformation($"Sending expiration reminder to {subscription.Email}.");
//                    await _emailService.SendSubscriptionExpirationReminderAsync(subscription.Email, subscription.ExpiryDate);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "An error occurred while checking for expiring subscriptions.");
//            }
//        }

//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Subscription Expiration Reminder Service stopped.");

//            _timer?.Change(Timeout.Infinite, 0);

//            return Task.CompletedTask;
//        }

//        public void Dispose()
//        {
//            _timer?.Dispose();
//        }
//    }
//}
