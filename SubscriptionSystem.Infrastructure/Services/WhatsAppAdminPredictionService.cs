using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.DTOs;
using SubscriptionSystem.Application.Interfaces;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// Service that handles incoming admin WhatsApp messages: verifies admin number,
    /// parses message and calls prediction APIs (via IPredictionService).
    /// </summary>
    public class WhatsAppAdminPredictionService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppAdminPredictionService> _logger;
        private readonly WhatsAppAdminCommandParser _parser;
        private readonly IPredictionService _predictionService;
        private readonly IWhatsAppProvider _whatsAppProvider;
        private readonly PredictionNotificationService _predictionNotificationService;

        public WhatsAppAdminPredictionService(
            IConfiguration configuration,
            ILogger<WhatsAppAdminPredictionService> logger,
            WhatsAppAdminCommandParser parser,
            IPredictionService predictionService,
            IWhatsAppProvider whatsAppProvider,
            PredictionNotificationService predictionNotificationService)
        {
            _configuration = configuration;
            _logger = logger;
            _parser = parser;
            _predictionService = predictionService;
            _whatsAppProvider = whatsAppProvider;
            _predictionNotificationService = predictionNotificationService;
        }

        public async Task<HandlerResult> HandleIncomingAsync(string from, string text)
        {
            try
            {
                var adminNumberConfigured = _configuration["WhatsApp:AdminNumber"] ?? _configuration["WhatsApp:AdminPhoneNumber"];
                if (string.IsNullOrWhiteSpace(adminNumberConfigured))
                {
                    _logger.LogWarning("Admin WhatsApp number not configured (WhatsApp:AdminNumber)");
                    return HandlerResult.Error("Admin number not configured");
                }

                var normalizedIncoming = NormalizeToE164(from);
                var normalizedAdmin = NormalizeToE164(adminNumberConfigured);

                if (!string.Equals(normalizedIncoming, normalizedAdmin, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Incoming WhatsApp message from {From} rejected: not admin number", from);
                    return HandlerResult.Error("Sender not authorized");
                }

                var parse = _parser.Parse(text);
                if (!parse.IsValid)
                {
                    var err = string.Join(';', parse.Errors);
                    _logger.LogWarning("Failed to parse admin message: {Errors}", err);
                    // Send back help text to admin
                    await _whatsAppProvider.SendMessageAsync(normalizedAdmin, GetHelpText());
                    return HandlerResult.Error(err);
                }

                if (parse.Detailed is not null)
                {
                    var dto = parse.Detailed;
                    var res = await _predictionService.CreateDetailedPredictionAsync(dto);
                    if (res.IsSuccess)
                    {
                        await _whatsAppProvider.SendMessageAsync(normalizedAdmin, $"Detailed prediction created: {res.Data}");
                        // Notify active subscribers via WhatsApp
                        try
                        {
                            await _predictionNotificationService.NotifySubscribedUsersAsync(dto.Team1, dto.Team2, dto.MatchDate.ToString("u"), dto.PredictedOutcome, dto.ConfidenceLevel.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to notify subscribers after detailed prediction creation");
                        }
                        return HandlerResult.Success($"Created:{res.Data}");
                    }
                    await _whatsAppProvider.SendMessageAsync(normalizedAdmin, $"Failed: {res.ErrorMessage}");
                    return HandlerResult.Error(res.ErrorMessage ?? "Failed to create detailed prediction");
                }

                if (parse.Simple is not null)
                {
                    var simple = parse.Simple;
                    var res = await _predictionService.CreateSimplePredictionAsync(simple);
                    if (res.IsSuccess)
                    {
                        await _whatsAppProvider.SendMessageAsync(normalizedAdmin, $"Simple prediction created: {res.Data}");
                        // Notify active subscribers via WhatsApp
                        try
                        {
                            var teams = simple.AlphanumericPrediction ?? string.Empty;
                            await _predictionNotificationService.NotifySubscribedUsersAsync(teams, string.Empty, DateTime.UtcNow.ToString("u"), parse.PredictedOutcome ?? (res.Data?.ToString() ?? teams), parse.Confidence.ToString());
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to notify subscribers after simple prediction creation");
                        }
                        return HandlerResult.Success($"Created:{res.Data}");
                    }
                    await _whatsAppProvider.SendMessageAsync(normalizedAdmin, $"Failed: {res.ErrorMessage}");
                    return HandlerResult.Error(res.ErrorMessage ?? "Failed to create simple prediction");
                }

                return HandlerResult.Error("No actionable command found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling incoming admin WhatsApp message");
                return HandlerResult.Error(ex.Message);
            }
        }

        private static string NormalizeToE164(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return string.Empty;
            var cleaned = System.Text.RegularExpressions.Regex.Replace(phoneNumber, "[^\\d+]", "");
            if (!cleaned.StartsWith("+"))
            {
                if (cleaned.StartsWith("0") && cleaned.Length > 1)
                    cleaned = "+234" + cleaned.Substring(1);
                else if (cleaned.StartsWith("234"))
                    cleaned = "+" + cleaned;
                else
                    cleaned = "+" + cleaned;
            }
            return cleaned;
        }

        private static string GetHelpText()
        {
            return "Admin command format:\n/detailed\nTournament: <name>\nTeam1: <team>\nTeam2: <team>\nMatchDate: YYYY-MM-DDTHH:MM or dd/MM/yyyy HH:mm\nMatchDetails: <text>\nConfidenceLevel: 72\nPredictedOutcome: <text>\n\nOr:\n/simple\nTeam1: <team>\nTeam2: <team>\nPredictedOutcome: <text>\nConfidenceLevel: 72";
        }

        public record HandlerResult(bool IsSuccess, string Message)
        {
            public static HandlerResult Success(string msg) => new HandlerResult(true, msg);
            public static HandlerResult Error(string msg) => new HandlerResult(false, msg);
        }
    }
}
