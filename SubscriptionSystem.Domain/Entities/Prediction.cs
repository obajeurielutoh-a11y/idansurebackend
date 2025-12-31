using System;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Domain.Entities
{
    public class Prediction
    {
        [Key] // Use this attribute if not using Fluent API
        public Guid Id { get; set; }
        //public string Id { get; set; }
        public string Tournament { get; set; }
        public string Team1 { get; set; }
        public string Team2 { get; set; }
        public DateTime MatchDate { get; set; }
        public string MatchDetails { get; set; }
        public string? NonAlphanumericDetails { get; set; }

        public bool IsDetailed { get; set; }
        public bool IsPromotional { get; set; }
        public MatchOutcome? Outcome { get; set; }
        public DateTime CreatedAt { get; set; }
        // Team performance details were removed from the model and DB
        public int ConfidenceLevel { get; set; }
        public string PredictedOutcome { get; set; }
    }



    public enum MatchOutcome
    {
        Win,
        Loss,
        Pending
        
    }
}

