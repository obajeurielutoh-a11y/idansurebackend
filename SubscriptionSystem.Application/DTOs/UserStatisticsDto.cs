namespace SubscriptionSystem.Application.DTOs
{
    public class UserStatisticsDto
    {
        public int TotalRegisteredUsers { get; set; }
        public int TotalSubscribedUsers { get; set; }
        public decimal TotalAmountPaidByUsers { get; set; }
        public object[] UserGrowthData { get; set; }
        public object[] RevenueData { get; set; }
        public object[] SubscriberDistribution { get; set; }
    }
}

