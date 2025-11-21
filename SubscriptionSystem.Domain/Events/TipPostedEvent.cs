using System;

namespace SubscriptionSystem.Domain.Events
{
    public class TipPostedEvent : DomainEvent
    {
        public Guid PredictionId { get; }
        public bool IsDetailed { get; }
        public bool IsPromotional { get; }
        public DateTime MatchDate { get; }
        public string Tournament { get; }
        public string Team1 { get; }
        public string Team2 { get; }

        public TipPostedEvent(Guid predictionId, bool isDetailed, bool isPromotional, DateTime matchDate, string tournament, string team1, string team2)
        {
            PredictionId = predictionId;
            IsDetailed = isDetailed;
            IsPromotional = isPromotional;
            MatchDate = matchDate;
            Tournament = tournament;
            Team1 = team1;
            Team2 = team2;
        }
    }
}
