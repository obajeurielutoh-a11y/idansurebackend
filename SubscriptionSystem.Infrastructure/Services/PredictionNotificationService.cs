using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// Service for sending WhatsApp notifications about prediction updates to subscribed users
    /// </summary>
    public class PredictionNotificationService
    {
        private readonly IWhatsAppProvider _whatsAppProvider;
        private readonly IUserManagementService _userManagementService;
        private readonly ISubscriptionRepository _subscriptionRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<PredictionNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public PredictionNotificationService(
            IWhatsAppProvider whatsAppProvider,
            IUserManagementService userManagementService,
            ISubscriptionRepository subscriptionRepository,
            IUserRepository userRepository,
            ILogger<PredictionNotificationService> logger,
            IConfiguration configuration)
        {
            _whatsAppProvider = whatsAppProvider;
            _userManagementService = userManagementService;
            _subscriptionRepository = subscriptionRepository;
            _userRepository = userRepository;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Send WhatsApp notification to all subscribed users about a new/updated prediction
        /// </summary>
        public async Task NotifySubscribedUsersAsync(string team1, string team2, string matchDate, string prediction, string confidenceScore, string actionType = "new")
        {
            try
            {
                var baseUrl = _configuration["Authentication:Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://www.idansure.com";
                
                // Build notification message in multiple languages
                var messages = new Dictionary<string, string>()
                {
                    {
                        "en", 
                        $"🎯 IdanSure Prediction Update!\n\n" +
                        $"{team1} vs {team2}\n" +
                        $"Date: {matchDate}\n" +
                        $"Prediction: {prediction}\n" +
                        $"Confidence: {confidenceScore}\n\n" +
                        $"View full details: {baseUrl}/predictions\n\n" +
                        $"Stay sharp! 💪"
                    },
                    {
                        "ig",
                        $"🎯 IdanSure Ọrụ Mmara Mmalite!\n\n" +
                        $"{team1} vs {team2}\n" +
                        $"Oge: {matchDate}\n" +
                        $"Mmara: {prediction}\n" +
                        $"Ntụkwasị obi: {confidenceScore}\n\n" +
                        $"Lelee ihe zuru ezu: {baseUrl}/predictions\n\n" +
                        $"Nwei ike! 💪"
                    },
                    {
                        "ha",
                        $"🎯 IdanSure Sabuwar Labari!\n\n" +
                        $"{team1} vs {team2}\n" +
                        $"Lokaci: {matchDate}\n" +
                        $"Tsinkaye: {prediction}\n" +
                        $"Tabbataci: {confidenceScore}\n\n" +
                        $"Duba cikakke: {baseUrl}/predictions\n\n" +
                        $"Ka ci gaba! 💪"
                    },
                    {
                        "yo",
                        $"🎯 IdanSure Ìfẹ́ Tuntun!\n\n" +
                        $"{team1} vs {team2}\n" +
                        $"Àsìkò: {matchDate}\n" +
                        $"Èlò: {prediction}\n" +
                        $"Ìgbagbọ́: {confidenceScore}\n\n" +
                        $"Wò gbogbo àkọ́kọ́: {baseUrl}/predictions\n\n" +
                        $"Gbé ara e lójú! 💪"
                    },
                    {
                        "pcm",
                        $"🎯 IdanSure New Update!\n\n" +
                        $"{team1} vs {team2}\n" +
                        $"Time: {matchDate}\n" +
                        $"My Prediction: {prediction}\n" +
                        $"Confidence Level: {confidenceScore}\n\n" +
                        $"Check full gist: {baseUrl}/predictions\n\n" +
                        $"Keep sharp now! 💪"
                    }
                };

                _logger.LogInformation("Preparing to send WhatsApp prediction updates to active subscribers");

                // Optional: developer breakpoint for testing a non-subscribed user
                var breakpointEnabled = string.Equals(_configuration["WhatsApp:BreakpointEnabled"], "true", StringComparison.OrdinalIgnoreCase);
                var breakpointPhone = _configuration["WhatsApp:BreakpointTestPhone"];
                if (breakpointEnabled && !string.IsNullOrWhiteSpace(breakpointPhone))
                {
                    var bpPhone = breakpointPhone.StartsWith("+") ? breakpointPhone : "+" + breakpointPhone;
                    try
                    {
                        var bpUser = await _userRepository.GetUserByPhoneNumberAsync(bpPhone);
                        if (bpUser != null)
                        {
                            var bpActive = await _subscriptionRepository.GetActiveSubscriptionAsync(bpUser.Id);
                            if (bpActive == null)
                            {
                                _logger.LogWarning("Breakpoint hit for non-subscribed test user {Phone}. Pausing (Debugger.Break()).", bpPhone);
                                try { Debugger.Break(); } catch { /* ignore if no debugger attached */ }
                            }
                            else
                            {
                                _logger.LogInformation("Breakpoint test user {Phone} has active subscription; no breakpoint triggered.", bpPhone);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("Breakpoint test phone {Phone} did not match any user.", bpPhone);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error while attempting breakpoint test lookup for {Phone}", bpPhone);
                    }
                }

                // Query subscriptions and notify only active subscribers
                var allSubscriptions = await _subscriptionRepository.GetAllAsync();
                var activeSubs = allSubscriptions
                    .Where(s => s.IsActive && s.ExpiryDate > DateTime.UtcNow)
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToList();

                _logger.LogInformation("Found {Count} active subscriber userIds to notify", activeSubs.Count);

                foreach (var userId in activeSubs)
                {
                    try
                    {
                        var userDto = await _userManagementService.GetUserByIdAsync(userId);
                        if (userDto == null) continue;

                        var phoneNumber = userDto.WhatsAppPhoneNumber ?? userDto.PhoneNumber;
                        if (string.IsNullOrWhiteSpace(phoneNumber)) continue;

                        if (!phoneNumber.StartsWith("+")) phoneNumber = "+" + phoneNumber;

                        var lang = userDto.PreferredLanguage ?? "en";
                        if (!messages.TryGetValue(lang, out var messageToSend)) messageToSend = messages["en"];

                        await _whatsAppProvider.SendMessageAsync(phoneNumber, messageToSend);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to send WhatsApp notification to user {UserId}", userId);
                    }
                }

                _logger.LogInformation("Completed WhatsApp notification dispatch");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending WhatsApp prediction notifications");
                throw;
            }
        }

        /// <summary>
        /// Send a test WhatsApp notification to verify configuration
        /// </summary>
        public async Task SendTestNotificationAsync(string phoneNumber)
        {
            try
            {
                var message = $"🎯 IdanSure Test Notification\n\nHello! This is a test message to verify WhatsApp integration is working correctly.\n\nIf you receive this, you're all set to get prediction updates! 🎉";
                
                if (!phoneNumber.StartsWith("+"))
                {
                    phoneNumber = "+" + phoneNumber;
                }

                await _whatsAppProvider.SendMessageAsync(phoneNumber, message);
                _logger.LogInformation($"Test WhatsApp message sent to {phoneNumber}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send test WhatsApp message to {phoneNumber}");
                throw;
            }
        }
    }
}
