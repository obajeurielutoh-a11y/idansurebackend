namespace SubscriptionSystem.Application.DTOs
{
    public class PredictionResponseDto
    {
        
        public Guid Id { get; set; }
        public string Tournament { get; set; }
        public string Team1 { get; set; }
        public string Team2 { get; set; }
        public DateTime MatchDate { get; set; }
        public string MatchDetails { get; set; }
        public string NonAlphanumericDetails { get; set; }
        public bool IsDetailed { get; set; }
        public bool IsPromotional { get; set; }
        public DateTime CreatedAt { get; set; }
        public MatchOutcome? Outcome { get; set; }
    }
}

