using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.BackgroundServices
{
    public class SubscriptionExpirationCheckService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<SubscriptionExpirationCheckService> _logger;

        public SubscriptionExpirationCheckService(IServiceProvider services, ILogger<SubscriptionExpirationCheckService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Subscription expiration check service running at: {time}", DateTimeOffset.Now);

                using (var scope = _services.CreateScope())
                {
                    var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                    await subscriptionService.NotifyExpiringSubscriptionsAsync();
                }

                // Run every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}

