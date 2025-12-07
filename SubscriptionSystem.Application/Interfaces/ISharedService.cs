using System;

namespace SubscriptionSystem.Application.Interfaces
{
    public interface ISharedService
    {
        string GetApplicationName();
        DateTime GetCurrentTime();
        string FormatCurrency(decimal amount, string currencyCode);
    }
}

