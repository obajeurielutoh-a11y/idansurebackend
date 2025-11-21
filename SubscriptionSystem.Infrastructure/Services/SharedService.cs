using SubscriptionSystem.Application.Interfaces;
using System;
using System.Globalization;

namespace SubscriptionSystem.Infrastructure.Services
{
    public class SharedService : ISharedService
    {
        public string GetApplicationName()
        {
            return "Subscription System";
        }

        public DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }

        public string FormatCurrency(decimal amount, string currencyCode)
        {
            return amount.ToString("C", new CultureInfo(currencyCode));
        }
    }
}

