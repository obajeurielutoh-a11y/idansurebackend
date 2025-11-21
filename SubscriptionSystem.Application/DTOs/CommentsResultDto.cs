
namespace SubscriptionSystem.Application.DTOs
{
    public class CommentsResultDto
    {
        public List<MessageDto> Comments { get; set; }
        public int TotalCount { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
    }
}

