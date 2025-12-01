using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SubscriptionSystem.Domain.Entities
{
    /// <summary>
    /// Daily prediction tips sent to 100 naira subscribers
    /// </summary>
    [Table("DailyPredictions")]
    public class DailyPrediction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Team1 { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string Team2 { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string PredictionOutcome { get; set; } = string.Empty;

        /// <summary>
        /// The date this prediction is for (only today allowed)
        /// </summary>
        [Required]
        public DateTime PredictionDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Tracks which msisdn received which prediction (one per phone per day)
    /// </summary>
    [Table("PredictionDeliveryLog")]
    public class PredictionDeliveryLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Msisdn { get; set; } = string.Empty;

        [Required]
        public int PredictionId { get; set; }

        [ForeignKey(nameof(PredictionId))]
        public DailyPrediction? Prediction { get; set; }

        /// <summary>
        /// Date the prediction was sent (for one-per-day enforcement)
        /// </summary>
        [Required]
        public DateTime SentDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
