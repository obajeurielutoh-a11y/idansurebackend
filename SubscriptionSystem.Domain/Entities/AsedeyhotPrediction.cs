using SubscriptionSystem.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Domain.Entities
{
    public class AsedeyhotPrediction
    {
        [Key]
        public Guid Id { get; set; }

        [MaxLength(500)]
        public string? AlphanumericPrediction { get; set; }

        [MaxLength(1000)]
        public string? NonAlphanumericDetails { get; set; }

        [MaxLength(500)]
        public string? PredictedOutcome { get; set; }

        public bool IsPromotional { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool? IsWin { get; set; }

        public DateTime? ResultDate { get; set; }

        [MaxLength(1000)]
        public string? ResultDetails { get; set; }

        public bool IsSuccess { get; set; }

        public PredictionStatus Status { get; set; }
    }
}

