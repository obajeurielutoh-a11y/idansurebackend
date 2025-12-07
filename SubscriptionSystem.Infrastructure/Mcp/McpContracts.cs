using System.Text.Json.Serialization;

namespace SubscriptionSystem.Infrastructure.Mcp
{
    // Basic JSON-RPC style envelope used for requests coming from MCP clients
    public class McpRequest
    {
        [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString();
        [JsonPropertyName("method")] public string Method { get; set; } = string.Empty; // e.g. "checkSubscription"
        [JsonPropertyName("params")] public Dictionary<string, object?>? Params { get; set; }
    }

    // Standard success response
    public class McpResponse
    {
        [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
        [JsonPropertyName("result")] public object? Result { get; set; }
        [JsonPropertyName("error")] public McpError? Error { get; set; }
    }

    // Error body aligned with JSON-RPC style
    public class McpError
    {
        [JsonPropertyName("code")] public int Code { get; set; }
        [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
        [JsonPropertyName("data")] public object? Data { get; set; }
    }

    // Tool descriptor surfaced when a client asks for available tools
    public class McpToolDescriptor
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty; // unique tool name
        [JsonPropertyName("description")] public string Description { get; set; } = string.Empty; // human readable
        [JsonPropertyName("inputSchema")] public Dictionary<string, object?> InputSchema { get; set; } = new(); // simple schema metadata
    }

    // Wrapper for listing tools
    public class McpToolListResult
    {
        [JsonPropertyName("tools")] public List<McpToolDescriptor> Tools { get; set; } = new();
    }
}
