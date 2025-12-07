using System;

namespace SubscriptionSystem.Domain.Entities
{
    public class Comment
    {
        public string Id { get; set; }
        public string UserId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string ParentId { get; set; }
        public string ImageUrl { get; set; }
        public string VoiceNoteUrl { get; set; }
    }
}

