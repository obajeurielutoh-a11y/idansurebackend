using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SubscriptionSystem.Infrastructure.Services
{
    /// <summary>
    /// WhatsApp MCP (Model Context Protocol) Adapter for PraisonAI Agent
    /// Provides integration between IdanSure and WhatsApp Cloud API via Model Context Protocol
    /// Manages message routing, verification, and delivery tracking
    /// </summary>
    public class WhatsAppMCPAdapter
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<WhatsAppMCPAdapter> _logger;

        public WhatsAppMCPAdapter(IConfiguration configuration, ILogger<WhatsAppMCPAdapter> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// MCP Resource representing a WhatsApp recipient
        /// Conforms to Model Context Protocol resource schema
        /// </summary>
        public class WhatsAppRecipientMCP
        {
            public string? Uri { get; set; } // mcp://whatsapp/recipient/{phone_number}
            public string? PhoneNumber { get; set; }
            public string? PhoneNumberId { get; set; }
            public bool IsVerified { get; set; }
            public DateTime RegisteredAt { get; set; }
            public string? PreferredLanguage { get; set; }
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        /// <summary>
        /// MCP Resource representing a WhatsApp message
        /// </summary>
        public class WhatsAppMessageMCP
        {
            public string? Uri { get; set; } // mcp://whatsapp/message/{message_id}
            public string? MessageId { get; set; }
            public string? RecipientPhone { get; set; }
            public string? MessageBody { get; set; }
            public string? MessageType { get; set; } // text, image, document, etc.
            public DateTime SentAt { get; set; }
            public string? Status { get; set; } // sent, delivered, read, failed
            public Dictionary<string, object> Metadata { get; set; } = new();
        }

        /// <summary>
        /// MCP Resource representing a WhatsApp broadcast group
        /// </summary>
        public class WhatsAppBroadcastMCP
        {
            public string? Uri { get; set; } // mcp://whatsapp/broadcast/{broadcast_id}
            public string? BroadcastId { get; set; }
            public string? BroadcastName { get; set; }
            public List<string> RecipientPhones { get; set; } = new();
            public string? MessageTemplate { get; set; }
            public string? Language { get; set; }
            public int TotalRecipients { get; set; }
            public int SuccessfulDeliveries { get; set; }
            public int FailedDeliveries { get; set; }
            public DateTime CreatedAt { get; set; }
            public DateTime? CompletedAt { get; set; }
        }

        /// <summary>
        /// Create MCP Resource for a WhatsApp recipient
        /// </summary>
        public WhatsAppRecipientMCP CreateRecipientMCP(string phoneNumber, string language, Dictionary<string, object>? metadata = null)
        {
            var mcp = new WhatsAppRecipientMCP
            {
                PhoneNumber = phoneNumber,
                PhoneNumberId = _configuration["WhatsApp:PhoneNumberId"] ?? "unknown",
                Uri = $"mcp://whatsapp/recipient/{Uri.EscapeDataString(phoneNumber)}",
                IsVerified = false,
                RegisteredAt = DateTime.UtcNow,
                PreferredLanguage = language,
                Metadata = metadata ?? new()
            };

            return mcp;
        }

        /// <summary>
        /// Create MCP Resource for a WhatsApp message
        /// </summary>
        public WhatsAppMessageMCP CreateMessageMCP(
            string messageId,
            string recipientPhone,
            string messageBody,
            string messageType = "text",
            Dictionary<string, object>? metadata = null)
        {
            var mcp = new WhatsAppMessageMCP
            {
                MessageId = messageId,
                RecipientPhone = recipientPhone,
                MessageBody = messageBody,
                MessageType = messageType,
                Uri = $"mcp://whatsapp/message/{messageId}",
                SentAt = DateTime.UtcNow,
                Status = "sent",
                Metadata = metadata ?? new()
            };

            return mcp;
        }

        /// <summary>
        /// Create MCP Resource for a WhatsApp broadcast group
        /// </summary>
        public WhatsAppBroadcastMCP CreateBroadcastMCP(
            string broadcastId,
            string broadcastName,
            List<string> recipientPhones,
            string messageTemplate,
            string language)
        {
            var mcp = new WhatsAppBroadcastMCP
            {
                BroadcastId = broadcastId,
                BroadcastName = broadcastName,
                Uri = $"mcp://whatsapp/broadcast/{broadcastId}",
                RecipientPhones = recipientPhones,
                MessageTemplate = messageTemplate,
                Language = language,
                TotalRecipients = recipientPhones.Count,
                CreatedAt = DateTime.UtcNow
            };

            return mcp;
        }

        /// <summary>
        /// Verify WhatsApp number in MCP context
        /// </summary>
        public async Task<bool> VerifyRecipientMCPAsync(WhatsAppRecipientMCP recipient)
        {
            try
            {
                // In real implementation, call WhatsApp API verification
                // For now, simulate async verification
                await Task.Delay(100);
                
                recipient.IsVerified = true;
                _logger.LogInformation(
                    "MCP Recipient verified: {Phone}",
                    recipient.PhoneNumber);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify MCP recipient: {Phone}", recipient.PhoneNumber);
                return false;
            }
        }

        /// <summary>
        /// Queue MCP message for delivery via PraisonAI Agent
        /// </summary>
        public async Task<WhatsAppMessageMCP> QueueMessageMCPAsync(
            WhatsAppMessageMCP message,
            int retryCount = 3,
            int delayMilliseconds = 1000)
        {
            try
            {
                _logger.LogInformation(
                    "Queuing MCP message {MessageId} to {Phone}",
                    message.MessageId,
                    message.RecipientPhone);

                // Queue for delivery with retry logic
                var attempt = 0;
                while (attempt < retryCount)
                {
                    try
                    {
                        // Simulate delivery via MCP
                        await Task.Delay(delayMilliseconds);
                        message.Status = "delivered";
                        break;
                    }
                    catch (Exception ex)
                    {
                        attempt++;
                        if (attempt >= retryCount)
                        {
                            message.Status = "failed";
                            _logger.LogError(ex, "Failed to deliver MCP message after {Attempts} attempts", retryCount);
                            throw;
                        }
                        await Task.Delay(delayMilliseconds * (attempt + 1)); // Exponential backoff
                    }
                }

                return message;
            }
            catch (Exception ex)
            {
                message.Status = "failed";
                _logger.LogError(ex, "Failed to queue MCP message {MessageId}", message.MessageId);
                throw;
            }
        }

        /// <summary>
        /// Execute MCP broadcast to multiple recipients
        /// Coordinated by PraisonAI Agent for efficient delivery
        /// </summary>
        public async Task<WhatsAppBroadcastMCP> ExecuteBroadcastMCPAsync(WhatsAppBroadcastMCP broadcast)
        {
            try
            {
                _logger.LogInformation(
                    "Executing MCP broadcast {BroadcastId} to {Count} recipients",
                    broadcast.BroadcastId,
                    broadcast.TotalRecipients);

                var successCount = 0;
                var failureCount = 0;

                foreach (var phone in broadcast.RecipientPhones)
                {
                    try
                    {
                        var messageId = Guid.NewGuid().ToString();
                        var message = CreateMessageMCP(messageId, phone, broadcast.MessageTemplate ?? "", "text");
                        
                        await QueueMessageMCPAsync(message);
                        
                        if (message.Status == "delivered")
                            successCount++;
                        else
                            failureCount++;
                    }
                    catch (Exception ex)
                    {
                        failureCount++;
                        _logger.LogWarning(ex, "Failed to send to {Phone}", phone);
                    }
                }

                broadcast.SuccessfulDeliveries = successCount;
                broadcast.FailedDeliveries = failureCount;
                broadcast.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "MCP broadcast {BroadcastId} completed. Success: {Success}, Failed: {Failed}",
                    broadcast.BroadcastId,
                    successCount,
                    failureCount);

                return broadcast;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MCP broadcast {BroadcastId} failed", broadcast.BroadcastId);
                throw;
            }
        }

        /// <summary>
        /// Get MCP resource by URI
        /// </summary>
        public object? GetResourceByUri(string uri)
        {
            try
            {
                if (uri.StartsWith("mcp://whatsapp/recipient/"))
                {
                    var phone = Uri.UnescapeDataString(uri.Replace("mcp://whatsapp/recipient/", ""));
                    return new WhatsAppRecipientMCP { PhoneNumber = phone, Uri = uri };
                }
                else if (uri.StartsWith("mcp://whatsapp/message/"))
                {
                    var messageId = uri.Replace("mcp://whatsapp/message/", "");
                    return new WhatsAppMessageMCP { MessageId = messageId, Uri = uri };
                }
                else if (uri.StartsWith("mcp://whatsapp/broadcast/"))
                {
                    var broadcastId = uri.Replace("mcp://whatsapp/broadcast/", "");
                    return new WhatsAppBroadcastMCP { BroadcastId = broadcastId, Uri = uri };
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get MCP resource: {Uri}", uri);
                return null;
            }
        }

        /// <summary>
        /// List all MCP resources of a specific type
        /// </summary>
        public async Task<List<string>> ListMCPResourcesAsync(string resourceType)
        {
            var resources = new List<string>();

            try
            {
                _logger.LogInformation("Listing MCP resources of type: {Type}", resourceType);

                // In production, this would query your database
                // For now, return empty list as example
                
                return resources;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list MCP resources of type: {Type}", resourceType);
                return resources;
            }
        }
    }
}
