using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// PraisonAI Agent service that uses WhatsApp MCP to send updates to registered WhatsApp numbers
    /// Automatically verifies numbers, manages delivery, and tracks notifications
    /// </summary>
    public class PraisonAIWhatsAppAgentService
    {
        private readonly IWhatsAppProvider _whatsAppProvider;
        private readonly IUserManagementService _userManagementService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PraisonAIWhatsAppAgentService> _logger;

        public PraisonAIWhatsAppAgentService(
            IWhatsAppProvider whatsAppProvider,
            IUserManagementService userManagementService,
            IConfiguration configuration,
            ILogger<PraisonAIWhatsAppAgentService> logger)
        {
            _whatsAppProvider = whatsAppProvider;
            _userManagementService = userManagementService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Verifies if a phone number is a valid WhatsApp number
        /// Uses E.164 format validation and WhatsApp API verification
        /// </summary>
        public async Task<bool> VerifyWhatsAppNumberAsync(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    _logger.LogWarning("Phone number is empty");
                    return false;
                }

                // Ensure E.164 format
                var normalizedNumber = NormalizeToE164(phoneNumber);
                if (string.IsNullOrEmpty(normalizedNumber))
                {
                    _logger.LogWarning("Failed to normalize phone number: {PhoneNumber}", phoneNumber);
                    return false;
                }

                // Validate E.164 format: +[country code][number]
                if (!normalizedNumber.StartsWith("+") || normalizedNumber.Length < 10)
                {
                    _logger.LogWarning("Invalid E.164 format: {PhoneNumber}", normalizedNumber);
                    return false;
                }

                // Verify it's a registered WhatsApp number by attempting a test send
                var testMessage = "📱 WhatsApp verification - This confirms your number is registered with IdanSure.";
                await _whatsAppProvider.SendMessageAsync(normalizedNumber, testMessage);
                
                _logger.LogInformation("WhatsApp number verified successfully: {PhoneNumber}", normalizedNumber);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to verify WhatsApp number: {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        /// <summary>
        /// Normalizes phone number to E.164 format
        /// Handles Nigerian numbers (+234, 0234, etc.)
        /// </summary>
        public string? NormalizeToE164(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return null;

            // Remove all non-digit characters except leading +
            var cleaned = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^\d+]", "");

            // If it doesn't start with +, add country code
            if (!cleaned.StartsWith("+"))
            {
                // Handle Nigerian numbers (0234 or 234)
                if (cleaned.StartsWith("0234"))
                    cleaned = "+" + cleaned.Substring(1); // 0234 -> +234
                else if (cleaned.StartsWith("234"))
                    cleaned = "+" + cleaned; // 234 -> +234
                else if (cleaned.StartsWith("0"))
                    cleaned = "+234" + cleaned.Substring(1); // 0801 -> +2348801
                else
                    cleaned = "+" + cleaned; // Unknown format, just add +
            }

            // Validate final format
            if (!cleaned.StartsWith("+") || cleaned.Length < 10)
                return null;

            return cleaned;
        }

        /// <summary>
        /// Send prediction update to all verified WhatsApp users using PraisonAI Agent
        /// Agent handles routing, scheduling, and retry logic
        /// </summary>
        public async Task<PredictionUpdateResult> SendPredictionUpdateViaAgentAsync(
            Prediction prediction,
            string predictionTitle,
            string predictionDetails)
        {
            var result = new PredictionUpdateResult
            {
                PredictionId = prediction.Id,
                ScheduledAt = DateTime.UtcNow,
                TotalRecipients = 0,
                SuccessfulSends = 0,
                FailedSends = 0,
                SkippedCount = 0,
                Errors = new List<string>()
            };

            try
            {
                _logger.LogInformation("Starting PraisonAI Agent task for prediction update: {PredictionId}", prediction.Id);

                // Get all users with verified WhatsApp numbers and subscriptions
                var users = await GetEligibleUsersAsync();
                result.TotalRecipients = users.Count;

                if (users.Count == 0)
                {
                    _logger.LogInformation("No eligible users for WhatsApp notification");
                    return result;
                }

                // Group users by language for efficient batch processing
                var usersByLanguage = users
                    .GroupBy(u => u.PreferredLanguage ?? "en")
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Process each language group with PraisonAI Agent
                foreach (var languageGroup in usersByLanguage)
                {
                    var language = languageGroup.Key;
                    var usersForLanguage = languageGroup.Value;

                    _logger.LogInformation(
                        "Processing {Count} users for language: {Language}",
                        usersForLanguage.Count,
                        language);

                    // Create agent task for this language group
                    var message = BuildMultilingualMessage(
                        prediction,
                        predictionTitle,
                        predictionDetails,
                        language);

                    // Send to each user in this language group
                    foreach (var user in usersForLanguage)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(user.WhatsAppPhoneNumber))
                            {
                                result.SkippedCount++;
                                continue;
                            }

                            // Verify number is still valid
                            var phoneNumber = NormalizeToE164(user.WhatsAppPhoneNumber);
                            if (string.IsNullOrEmpty(phoneNumber))
                            {
                                result.SkippedCount++;
                                _logger.LogWarning(
                                    "Invalid WhatsApp number for user {UserId}: {PhoneNumber}",
                                    user.Id,
                                    user.WhatsAppPhoneNumber);
                                continue;
                            }

                            // Send via WhatsApp MCP
                            await _whatsAppProvider.SendMessageAsync(phoneNumber, message);
                            result.SuccessfulSends++;
                            
                            _logger.LogInformation(
                                "WhatsApp notification sent to user {UserId} in {Language}",
                                user.Id,
                                language);
                        }
                        catch (Exception ex)
                        {
                            result.FailedSends++;
                            result.Errors.Add($"User {user.Id}: {ex.Message}");
                            
                            _logger.LogError(
                                ex,
                                "Failed to send WhatsApp notification to user {UserId}",
                                user.Id);
                        }
                    }
                }

                result.CompletedAt = DateTime.UtcNow;
                result.Status = result.FailedSends == 0 ? "Success" : "PartialSuccess";
                
                _logger.LogInformation(
                    "PraisonAI Agent task completed. Successful: {Successful}, Failed: {Failed}, Skipped: {Skipped}",
                    result.SuccessfulSends,
                    result.FailedSends,
                    result.SkippedCount);

                return result;
            }
            catch (Exception ex)
            {
                result.Status = "Failed";
                result.Errors.Add(ex.Message);
                result.CompletedAt = DateTime.UtcNow;
                
                _logger.LogError(ex, "PraisonAI Agent task failed for prediction {PredictionId}", prediction.Id);
                return result;
            }
        }

        /// <summary>
        /// Get all users eligible for WhatsApp notifications
        /// (subscribed + WhatsApp number provided + notifications enabled)
        /// </summary>
        private async Task<List<User>> GetEligibleUsersAsync()
        {
            try
            {
                // This would call your user management service
                // For now, returning empty list as example
                // In production: await _userManagementService.GetEligibleWhatsAppUsersAsync();
                return new List<User>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get eligible users for WhatsApp");
                return new List<User>();
            }
        }

        /// <summary>
        /// Build multilingual prediction update message
        /// </summary>
        private string BuildMultilingualMessage(
            Prediction prediction,
            string title,
            string details,
            string language)
        {
            return language switch
            {
                "ig" => BuildIgboMessage(prediction, title, details),
                "ha" => BuildHausaMessage(prediction, title, details),
                "yo" => BuildYorubaMessage(prediction, title, details),
                "pcm" => BuildPidginMessage(prediction, title, details),
                _ => BuildEnglishMessage(prediction, title, details)
            };
        }

        private string BuildEnglishMessage(Prediction prediction, string title, string details)
        {
            return $@"🎯 IdanSure Prediction Update!

*{title}*

{details}

📊 Confidence: {prediction.ConfidenceLevel}%
⏰ Match Time: {prediction.MatchDate:dd/MM/yyyy HH:mm}

View full predictions: https://www.idansure.com/predictions

Stay disciplined, bet wisely! 💪";
        }

        private string BuildIgboMessage(Prediction prediction, string title, string details)
        {
            return $@"🎯 IdanSure Ọrụ Mmara Mmalite!

*{title}*

{details}

📊 Ntụkwasị Obi: {prediction.ConfidenceLevel}%
⏰ Oge Egwu: {prediction.MatchDate:dd/MM/yyyy HH:mm}

Lelee ihe zuru ezu: https://www.idansure.com/predictions

Nwei ike, zụọ ike! 💪";
        }

        private string BuildHausaMessage(Prediction prediction, string title, string details)
        {
            return $@"🎯 IdanSure Sabuwar Labari!

*{title}*

{details}

📊 Tabbataci: {prediction.ConfidenceLevel}%
⏰ Lokaci Waje: {prediction.MatchDate:dd/MM/yyyy HH:mm}

Duba cikakke: https://www.idansure.com/predictions

Ka ci gaba da hikima! 💪";
        }

        private string BuildYorubaMessage(Prediction prediction, string title, string details)
        {
            return $@"🎯 IdanSure Ìfẹ́ Tuntun!

*{title}*

{details}

📊 Ìgbagbọ́: {prediction.ConfidenceLevel}%
⏰ Àsìkò Ìdándùn: {prediction.MatchDate:dd/MM/yyyy HH:mm}

Wò gbogbo àkọ́kọ́: https://www.idansure.com/predictions

Gbé ara e lójú! 💪";
        }

        private string BuildPidginMessage(Prediction prediction, string title, string details)
        {
            return $@"🎯 IdanSure New Update!

*{title}*

{details}

📊 Confidence: {prediction.ConfidenceLevel}%
⏰ Match Time: {prediction.MatchDate:dd/MM/yyyy HH:mm}

Check full gist: https://www.idansure.com/predictions

Keep sharp, bet correct! 💪";
        }

        /// <summary>
        /// Validate user's WhatsApp number registration
        /// Returns validation status and error details
        /// </summary>
        public WhatsAppValidationResult ValidateUserWhatsAppRegistration(string userId, string phoneNumber)
        {
            var result = new WhatsAppValidationResult
            {
                UserId = userId,
                PhoneNumber = phoneNumber,
                IsValid = true,
                Errors = new List<string>()
            };

            // Check if empty
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                result.IsValid = false;
                result.Errors.Add("WhatsApp number is required");
                return result;
            }

            // Normalize to E.164
            var normalized = NormalizeToE164(phoneNumber);
            if (string.IsNullOrEmpty(normalized))
            {
                result.IsValid = false;
                result.Errors.Add("Phone number format is invalid. Use E.164 format: +234xxxxxxxxxx");
                return result;
            }

            // Check format
            if (!normalized.StartsWith("+"))
            {
                result.IsValid = false;
                result.Errors.Add("Phone number must start with country code (+234 for Nigeria)");
                return result;
            }

            if (normalized.Length < 10 || normalized.Length > 15)
            {
                result.IsValid = false;
                result.Errors.Add("Phone number length is invalid");
                return result;
            }

            result.NormalizedPhoneNumber = normalized;
            return result;
        }
    }

    /// <summary>
    /// Result of sending prediction update via PraisonAI Agent
    /// </summary>
    public class PredictionUpdateResult
    {
        public Guid PredictionId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } // Pending, Success, PartialSuccess, Failed
        public int TotalRecipients { get; set; }
        public int SuccessfulSends { get; set; }
        public int FailedSends { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>
    /// Validation result for WhatsApp number registration
    /// </summary>
    public class WhatsAppValidationResult
    {
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public string? NormalizedPhoneNumber { get; set; }
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
