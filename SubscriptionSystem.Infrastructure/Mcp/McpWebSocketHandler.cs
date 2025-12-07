using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace SubscriptionSystem.Infrastructure.Mcp
{
    public static class McpWebSocketHandler
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("WebSocket connection expected");
                return;
            }

            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

            var registry = context.RequestServices.GetService(typeof(IMcpToolRegistry)) as IMcpToolRegistry;
            if (registry == null)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, "Registry unavailable", context.RequestAborted);
                return;
            }
            var buffer = new byte[8192];

            while (webSocket.State == WebSocketState.Open)
            {
                var result = await ReceiveFullMessageAsync(webSocket, buffer, context.RequestAborted);
                if (result.CloseReceived)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", context.RequestAborted);
                    break;
                }

                if (result.Bytes.Count == 0) continue;
                var json = Encoding.UTF8.GetString(result.Bytes);

                McpResponse response;
                try
                {
                    var req = JsonSerializer.Deserialize<McpRequest>(json, JsonOptions) ?? new McpRequest();
                    var res = new McpResponse { Id = req.Id };

                    if (string.Equals(req.Method, "listTools", StringComparison.OrdinalIgnoreCase))
                    {
                        res.Result = new McpToolListResult { Tools = registry.ListTools().ToList() };
                    }
                    else
                    {
                        var resultObj = await registry.InvokeAsync(req.Method, req.Params, context.RequestAborted);
                        res.Result = resultObj;
                    }

                    response = res;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    response = new McpResponse
                    {
                        Id = Guid.NewGuid().ToString(),
                        Error = new McpError { Code = -32603, Message = ex.Message }
                    };
                }

                var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, JsonOptions));
                await webSocket.SendAsync(payload, WebSocketMessageType.Text, true, context.RequestAborted);
            }
        }

        private static async Task<(ArraySegment<byte> Bytes, bool CloseReceived)> ReceiveFullMessageAsync(WebSocket socket, byte[] buffer, CancellationToken ct)
        {
            using var ms = new MemoryStream();
            while (true)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return (Array.Empty<byte>(), true);
                }
                ms.Write(buffer, 0, result.Count);
                if (result.EndOfMessage)
                {
                    return (new ArraySegment<byte>(ms.ToArray()), false);
                }
            }
        }
    }
}
