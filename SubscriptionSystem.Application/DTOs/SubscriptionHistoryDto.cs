using SubscriptionSystem.Application.DTOs;
using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class SubscriptionHistoryDto
    {
        public string PlanType { get; set; }
        public decimal AmountPaid { get; set; }
        public string Currency { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }


}

public class PagedSubscriptionHistoryDto
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<SubscriptionHistoryDto> Subscriptions { get; set; }
}

