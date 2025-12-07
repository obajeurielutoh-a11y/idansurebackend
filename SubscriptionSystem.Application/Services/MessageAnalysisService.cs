using System.Text.RegularExpressions;
using SubscriptionSystem.Application.Interfaces;

namespace SubscriptionSystem.Application.Services
{
    public class MessageAnalysisService : IMessageAnalysisService
    {
        public MessageAnalysisResult Analyze(string message)
        {
            message = message ?? string.Empty;

            var lower = message.ToLowerInvariant();
            var result = new MessageAnalysisResult();

            // Tone heuristic
            if (Regex.IsMatch(lower, @"\b(refund|angry|upset|complain|fail(ed)?|scam|fraud)\b"))
                result.Tone = "negative";
            else if (Regex.IsMatch(lower, @"\b(thank(s| you)|great|awesome|love|appreciate)\b"))
                result.Tone = "positive";
            else if (Regex.IsMatch(lower, @"!{2,}|\burgent\b|\basap\b"))
                result.Tone = "urgent";
            else
                result.Tone = "neutral";

            // Scope heuristic
            if (Regex.IsMatch(lower, @"\b(payment|paystack|alatpay|card|transfer|receipt|reference)\b"))
                result.Scope = "payment";
            else if (Regex.IsMatch(lower, @"\bsubscription|subscribe|plan|renew(al)?|expiry|active\b"))
                result.Scope = "subscription";
            else if (Regex.IsMatch(lower, @"\bfootball|match|league|prediction|odds|acca|bet\b"))
                result.Scope = "football";
            else
                result.Scope = "general";

            // Context summary (very short gist)
            result.ContextSummary = Summarize(lower);
            return result;
        }

        private static string Summarize(string text)
        {
            // Take first sentence up to ~140 chars as a micro-summary
            var end = text.IndexOfAny(new[] { '.', '!', '?' });
            var candidate = end > 0 ? text[..end] : text;
            candidate = Regex.Replace(candidate, @"\s+", " ").Trim();
            return candidate.Length <= 140 ? candidate : candidate[..140];
        }
    }
}
