// File: SubscriptionSystem.Application/DTOs/AdminDashboardDto.cs
using System.Collections.Generic;

namespace SubscriptionSystem.Application.DTOs
{
    public class AdminDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public int TotalTransactions { get; set; }
        public int ActiveSubscribers { get; set; }
        public List<GatewayStatDto> GatewayBreakdown { get; set; }
        public List<PlanTypeStatDto> PlanTypeBreakdown { get; set; }

        // Subscription counts
        public int TotalSubscribers { get; set; }
     
        public int ExpiredSubscribers { get; set; }

        // Breakdowns
       
        // Optional: Add subscription plan breakdown
        public List<SubscriptionPlanStatDto> SubscriptionPlanBreakdown { get; set; }

        public class SubscriptionPlanStatDto
        {
            public string PlanType { get; set; }
            public int TotalCount { get; set; }
            public int ActiveCount { get; set; }
            public int ExpiredCount { get; set; }
        }
    }
}