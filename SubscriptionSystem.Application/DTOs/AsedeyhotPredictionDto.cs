using SubscriptionSystem.Domain.Enums;
using System;

namespace SubscriptionSystem.Application.DTOs
{
    public class AsedeyhotPredictionDto
    {
        public Guid Id { get; set; }
        public string AlphanumericPrediction { get; set; }
        public string NonAlphanumericDetails { get; set; }
        public string PredictedOutcome { get; set; }
        public bool IsPromotional { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool? IsWin { get; set; }
        public DateTime? ResultDate { get; set; }
        public string ResultDetails { get; set; }
        public bool IsSuccess { get; set; }
        public PredictionStatus Status { get; set; }
    }
}

