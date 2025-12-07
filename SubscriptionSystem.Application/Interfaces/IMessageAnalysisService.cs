namespace SubscriptionSystem.Application.Interfaces
{
    public interface IMessageAnalysisService
    {
        MessageAnalysisResult Analyze(string message);
    }

    public class MessageAnalysisResult
    {
        public string Tone { get; set; } = "neutral"; // e.g. positive, negative, urgent, formal, casual
        public string Scope { get; set; } = "general"; // e.g. football, payment, subscription, account
        public string ContextSummary { get; set; } = string.Empty; // short distilled summary
    }
}
