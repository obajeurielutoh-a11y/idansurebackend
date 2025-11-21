namespace SubscriptionSystem.Application.DTOs
{
    public class PagedSubscriptionHistoryDto
    {
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public List<SubscriptionHistoryDto> Subscriptions { get; set; }
    }

}
