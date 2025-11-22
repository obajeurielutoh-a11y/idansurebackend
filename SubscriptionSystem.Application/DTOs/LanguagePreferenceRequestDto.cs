using System.ComponentModel.DataAnnotations;

namespace SubscriptionSystem.Application.DTOs
{
    public class LanguagePreferenceRequestDto
    {
        [Required]
        public string Language { get; set; } // en, ig, ha, yo, pcm

        [Phone]
        public string? WhatsAppPhoneNumber { get; set; }
    }
}
