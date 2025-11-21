using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Infrastructure.Mcp;
using SubscriptionSystem.Domain.Events;
using System.Text.Json;
using SubscriptionSystem.Application.Services;

namespace SubscriptionSystem.Infrastructure.Mcp
{
    public interface IMcpToolRegistry
    {
        IReadOnlyCollection<McpToolDescriptor> ListTools();
        Task<object?> InvokeAsync(string method, Dictionary<string, object?>? @params, CancellationToken ct);
    }

    public class McpToolRegistry : IMcpToolRegistry
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IPaymentService _paymentService;
        private readonly IDomainEventPublisher _eventPublisher;

        private readonly List<McpToolDescriptor> _tools;

        public McpToolRegistry(ISubscriptionService subscriptionService,
                                IPaymentService paymentService,
                                IDomainEventPublisher eventPublisher)
        {
            _subscriptionService = subscriptionService;
            _paymentService = paymentService;
            _eventPublisher = eventPublisher;
            _tools = new List<McpToolDescriptor>
            {
                new McpToolDescriptor
                {
                    Name = "checkSubscription",
                    Description = "Check if a user (by email) has an active subscription.",
                    InputSchema = new Dictionary<string, object?>
                    {
                        {"type", "object"},
                        {"properties", new Dictionary<string, object?> { {"email", new Dictionary<string, object?>{{"type","string"}} } }},
                        {"required", new []{"email"} }
                    }
                },
                new McpToolDescriptor
                {
                    Name = "createPaymentIntent",
                    Description = "Create a payment intent (stub) for a subscription purchase.",
                    InputSchema = new Dictionary<string, object?>
                    {
                        {"type", "object"},
                        {"properties", new Dictionary<string, object?> {
                            {"email", new Dictionary<string, object?>{{"type","string"}}},
                            {"plan", new Dictionary<string, object?>{{"type","string"}}},
                            {"amount", new Dictionary<string, object?>{{"type","number"}}}
                        }},
                        {"required", new []{"email","plan","amount"} }
                    }
                },
                new McpToolDescriptor
                {
                    Name = "postTipNotification",
                    Description = "Broadcast a betting tip notification via domain event to WhatsApp subscribers.",
                    InputSchema = new Dictionary<string, object?>
                    {
                        {"type", "object"},
                        {"properties", new Dictionary<string, object?> {
                            {"title", new Dictionary<string, object?>{{"type","string"}}},
                            {"content", new Dictionary<string, object?>{{"type","string"}}}
                        }},
                        {"required", new []{"title","content"} }
                    }
                }
            };
        }

        public IReadOnlyCollection<McpToolDescriptor> ListTools() => _tools;

        public async Task<object?> InvokeAsync(string method, Dictionary<string, object?>? @params, CancellationToken ct)
        {
            method = method ?? string.Empty;
            switch (method)
            {
                case "listTools":
                    return new McpToolListResult { Tools = _tools };
                case "checkSubscription":
                    {
                        var email = @params?.GetValueOrDefault("email") as string;
                        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("email is required");
                        var statusResult = await _subscriptionService.GetSubscriptionStatusAsync(email);
                        var status = statusResult.Data;
                        bool active = status?.IsActive ?? false;
                        return new { active, status };
                    }
                case "createPaymentIntent":
                    {
                        // Stub: in future integrate gateways; now just echo payload with idempotency token.
                        var email = @params?.GetValueOrDefault("email") as string ?? throw new ArgumentException("email required");
                        var plan = @params?.GetValueOrDefault("plan") as string ?? throw new ArgumentException("plan required");
                        var amountRaw = @params?.GetValueOrDefault("amount");
                        var amount = amountRaw is JsonElement je && je.ValueKind == JsonValueKind.Number ? je.GetDecimal() : Convert.ToDecimal(amountRaw);
                        return new { intentId = Guid.NewGuid().ToString("N"), email, plan, amount, createdAt = DateTime.UtcNow };
                    }
                case "postTipNotification":
                    {
                        // Minimal broadcast; use placeholders for required fields
                        var title = @params?.GetValueOrDefault("title") as string ?? throw new ArgumentException("title required");
                        var content = @params?.GetValueOrDefault("content") as string ?? throw new ArgumentException("content required");
                        var evt = new TipPostedEvent(Guid.NewGuid(), isDetailed: false, isPromotional: false, matchDate: DateTime.UtcNow,
                            tournament: title, team1: content, team2: "");
                        await _eventPublisher.PublishAsync(evt);
                        return new { dispatched = true, title, content };
                    }
                default:
                    throw new InvalidOperationException($"Unknown method '{method}'");
            }
        }
    }
}
