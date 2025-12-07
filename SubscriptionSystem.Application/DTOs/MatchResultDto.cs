using System;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public enum MatchOutcome
    {
        Win,
        Loss,
          // Adding Draw as a possible outcome
    }

    //public class MatchResultDto
    //{
    //    public string PredictionId { get; set; }
    //    public MatchOutcome Outcome { get; set; }
    //}
    public class MatchResultDto
    {
        [Required]
        public string PredictionId { get; set; }

        [Required]
        public Domain.Entities.MatchOutcome Outcome { get; set; }
    }
}


