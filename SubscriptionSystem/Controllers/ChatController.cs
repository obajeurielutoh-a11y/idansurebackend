using Microsoft.AspNetCore.Mvc;
using SubscriptionSystem.Application.Interfaces;
using SubscriptionSystem.Application.DTOs;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace SubscriptionSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "UserWithIdHeader")]
    public class ChatController : ControllerBase
    {
    private readonly IGroupChatService _groupChatService;
    private readonly IUserManagementService _userManagementService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly IAiChatProvider _aiChatProvider;
    private readonly IAudioService _audioService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ILogger<ChatController> _logger;
    private readonly IConfiguration _configuration;
    private readonly IDistributedCache _cache;
    private readonly IPaymentService _paymentService;
    private readonly string _uploadPath;
    private const int MaxAiResponseChars = 500; // limit AI response length

        public ChatController(
            IGroupChatService groupChatService,
            IUserManagementService userManagementService,
            ISubscriptionService subscriptionService,
            ILogger<ChatController> logger,
            IWebHostEnvironment environment,
            IAiChatProvider aiChatProvider,
            IAudioService audioService,
            ITranscriptionService transcriptionService,
            IConfiguration configuration,
            IDistributedCache cache,
            IPaymentService paymentService)
        {
            _groupChatService = groupChatService;
            _userManagementService = userManagementService;
            _subscriptionService = subscriptionService;
            _aiChatProvider = aiChatProvider;
            _audioService = audioService;
            _transcriptionService = transcriptionService;
            _logger = logger;
            _configuration = configuration;
            _cache = cache;
            _paymentService = paymentService;
            _uploadPath = Path.Combine(environment.WebRootPath, "uploads");
        }

        private async Task<bool> HasActiveSubscriptionAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("User email is missing");
                return false;
            }

            var user = await _userManagementService.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning($"User with email {email} not found");
                return false;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning($"User with email {email} is not active");
                return false;
            }

            // Properly verify active subscription via SubscriptionService
            try
            {
                return await _subscriptionService.HasActiveSubscriptionAsync(user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to verify subscription status for {Email}", email);
                return false;
            }
        }

        private async Task<bool> HasActiveSubscriptionByUserIdAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return false;
            try { return await _subscriptionService.HasActiveSubscriptionAsync(userId); }
            catch (Exception ex) { _logger.LogError(ex, "Subscription check failed for user {UserId}", userId); return false; }
        }

        private string? GetCurrentUserId()
        {
            // Prefer NameIdentifier, then custom 'sub' or 'userId' claims
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User?.FindFirst("sub")?.Value
                         ?? User?.FindFirst("userId")?.Value;
            return userId;
        }

    

        [HttpPost("ai")]
        public async Task<IActionResult> ChatWithAi([FromBody] AiChatRequestDto request, [FromServices] IMessageAnalysisService analysisService)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return BadRequest(new { message = "message is required" });

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(new { message = "Authentication required" });

            // per-user daily cap
            var cap = Math.Max(1, _configuration.GetValue<int>("Ai:DailyCap", 50));
            var rateKey = $"ai-cap:{currentUserId}:{DateTime.UtcNow:yyyyMMdd}";
            var countStr = await SafeGetStringAsync(rateKey);
            var currentCount = int.TryParse(countStr, out var parsed) ? parsed : 0;
            if (currentCount >= cap)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new AiChatResponseDto
                {
                    RequiresSubscription = false,
                    Message = $"Daily AI chat limit reached ({cap}). Try again tomorrow."
                });
            }

            // Check subscription status
            var hasSub = await HasActiveSubscriptionByUserIdAsync(currentUserId);

            // Track free conversations per user (allow 3 free conversations before asking for subscription)
            var freeConversationKey = $"ai-free-conv:{currentUserId}:{DateTime.UtcNow:yyyyMMdd}";
            var freeConvStr = await SafeGetStringAsync(freeConversationKey);
            var freeConversationCount = int.TryParse(freeConvStr, out var freeCount) ? freeCount : 0;
            var maxFreeConversations = _configuration.GetValue<int>("Ai:MaxFreeConversations", 3);
            var shouldPromptPayment = !hasSub && freeConversationCount >= maxFreeConversations;

            try
            {
                // Derive tone/scope/context if not provided
                var analysis = analysisService.Analyze(request.Message!);
                var tone = string.IsNullOrWhiteSpace(request.Tone) ? analysis.Tone : request.Tone;
                var scope = string.IsNullOrWhiteSpace(request.Scope) ? analysis.Scope : request.Scope;
                var context = string.IsNullOrWhiteSpace(request.Context) ? analysis.ContextSummary : request.Context;

                var aiText = await _aiChatProvider.GetResponseAsync(currentUserId, request.Message!, tone, scope, context);

                // For unsubscribed users after free limit, cap the response to medium level insights
                if (shouldPromptPayment && !string.IsNullOrEmpty(aiText))
                {
                    // Cap to medium level response (first 300 chars of analysis)
                    if (aiText.Length > 300)
                    {
                        aiText = aiText.Substring(0, 300) + "...\n\n💡 Want deeper insights and real-time alerts? Subscribe to IdanSure for full predictions and expert analysis!";
                    }
                }
                else if (!string.IsNullOrEmpty(aiText) && aiText.Length > MaxAiResponseChars)
                {
                    // For subscribed users, use full response up to max chars
                    aiText = aiText.Substring(0, MaxAiResponseChars);
                }

                // Increment conversation counter
                var expires = DateTimeOffset.UtcNow.Date.AddDays(1);
                await SafeSetStringAsync(freeConversationKey, (freeConversationCount + 1).ToString(), new DistributedCacheEntryOptions { AbsoluteExpiration = expires });
                
                // Also increment daily cap
                await SafeSetStringAsync(rateKey, (currentCount + 1).ToString(), new DistributedCacheEntryOptions { AbsoluteExpiration = expires });

                // log analytics to file (JSONL)
                await LogAiInteractionAsync(currentUserId, request.Message!, aiText);
                var response = new AiChatResponseDto
                {
                    RequiresSubscription = shouldPromptPayment,
                    Message = aiText,
                    ConversationsRemaining = !hasSub ? Math.Max(0, maxFreeConversations - freeConversationCount - 1) : null
                };

                // If user has exhausted free conversations, include subscription prompt
                if (shouldPromptPayment)
                {
                    string subscribeUrlFallback = _configuration["Authentication:Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://www.idansure.com";
                    subscribeUrlFallback = $"{subscribeUrlFallback}/subscribe";

                    try
                    {
                        var userDto = await _userManagementService.GetUserByIdAsync(currentUserId);
                        var email = userDto?.Email ?? string.Empty;
                        var amount = _configuration.GetValue<decimal?>("Payments:Plans:OneDay:Amount") ?? 100m;
                        var paystackReq = new PaystackInitializeRequestDto
                        {
                            Email = email,
                            Amount = amount,
                            CallbackUrl = _configuration["Paystack:CallbackUrl"] ?? string.Empty,
                            Metadata = new PaystackMetadata { UserId = currentUserId, PlanType = "OneDay" }
                        };
                        var init = await _paymentService.InitializePaystackPaymentAsync(paystackReq);
                        if (init != null && init.IsSuccess && init.Data?.Data?.AuthorizationUrl != null)
                        {
                            response.SubscribeUrl = init.Data.Data.AuthorizationUrl;
                            response.AgentDirective = "prompt_payment_friendly";
                        }
                        else
                        {
                            response.SubscribeUrl = subscribeUrlFallback;
                            response.AgentDirective = "prompt_payment_friendly";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to initialize Paystack checkout for user {UserId}", currentUserId);
                        response.SubscribeUrl = subscribeUrlFallback;
                        response.AgentDirective = "prompt_payment_friendly";
                    }
                }

                // Only generate TTS for subscribed users to control costs
                if (request.ReturnVoice == true && hasSub)
                {
                    try
                    {
                        var voice = string.IsNullOrWhiteSpace(request.VoiceStyle) ? _configuration["Voice:DefaultVoice"] ?? "Matthew" : request.VoiceStyle;
                        var s3Prefix = $"aichat/{currentUserId}/{DateTime.UtcNow:yyyyMMdd}";
                        var url = await _audioService.SynthesizeToS3Async(aiText, voice, s3Prefix);
                        response.VoiceUrl = url;
                    }
                    catch (Exception ttsEx)
                    {
                        _logger.LogWarning(ttsEx, "TTS failed; returning text-only");
                    }
                }
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI chat failed");

                // Friendly fallback on common network/DNS failures so users don't get a hard 500
                var isNameResolution =
                    ex.Message?.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) == true ||
                    (ex.InnerException?.Message?.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) == true) ||
                    ex is HttpRequestException;

                if (isNameResolution)
                {
                    var fallbackMsg = "I'm having trouble reaching the AI service right now. Please try again in a bit.";
                    var response = new AiChatResponseDto
                    {
                        RequiresSubscription = shouldPromptPayment,
                        Message = fallbackMsg,
                        AgentDirective = shouldPromptPayment ? "prompt_payment_friendly" : null
                    };

                    if (shouldPromptPayment)
                    {
                        string subscribeUrlFallback = _configuration["Authentication:Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://www.idansure.com";
                        subscribeUrlFallback = $"{subscribeUrlFallback}/subscribe";

                        try
                        {
                            var userDto = await _userManagementService.GetUserByIdAsync(currentUserId);
                            var email = userDto?.Email ?? string.Empty;
                            var amount = _configuration.GetValue<decimal?>("Payments:Plans:OneDay:Amount") ?? 100m;
                            var paystackReq = new PaystackInitializeRequestDto
                            {
                                Email = email,
                                Amount = amount,
                                CallbackUrl = _configuration["Paystack:CallbackUrl"] ?? string.Empty,
                                Metadata = new PaystackMetadata { UserId = currentUserId, PlanType = "OneDay" }
                            };
                            var init = await _paymentService.InitializePaystackPaymentAsync(paystackReq);
                            if (init != null && init.IsSuccess && init.Data?.Data?.AuthorizationUrl != null)
                            {
                                response.SubscribeUrl = init.Data.Data.AuthorizationUrl;
                            }
                            else
                            {
                                response.SubscribeUrl = subscribeUrlFallback;
                            }
                        }
                        catch
                        {
                            response.SubscribeUrl = subscribeUrlFallback;
                        }
                    }

                    return Ok(response);
                }

                return StatusCode(500, new { message = "AI chat failed", error = ex.Message });
            }
        }

        [HttpPost("ai/voice")]
        [RequestSizeLimit(20_000_000)] // ~20MB
        public async Task<IActionResult> ChatWithAiVoice([FromForm] AiVoiceChatRequest request, [FromServices] IMessageAnalysisService analysisService)
        {
            if (request == null || request.Audio == null || request.Audio.Length == 0)
                return BadRequest(new { message = "audio is required" });

            var currentUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(currentUserId))
                return Unauthorized(new { message = "Authentication required" });

            // Subscription gating for signed-in user
            var hasSub = await HasActiveSubscriptionByUserIdAsync(currentUserId);
            if (!hasSub)
            {
                // Try to initialize a Paystack payment so the client can redirect straight to checkout
                string subscribeUrlFallback = _configuration["Authentication:Frontend:BaseUrl"]?.TrimEnd('/') ?? "https://www.idansure.com";
                subscribeUrlFallback = $"{subscribeUrlFallback}/subscribe";

                try
                {
                    var userDto = await _userManagementService.GetUserByIdAsync(currentUserId);
                    var email = userDto?.Email ?? string.Empty;

                    // Default to a OneDay plan amount (100 NGN) if no config provided
                    var amount = _configuration.GetValue<decimal?>("Payments:Plans:OneDay:Amount") ?? 100m;

                    var paystackReq = new PaystackInitializeRequestDto
                    {
                        Email = email,
                        Amount = amount,
                        CallbackUrl = _configuration["Paystack:CallbackUrl"] ?? string.Empty,
                        Metadata = new PaystackMetadata { UserId = currentUserId, PlanType = "OneDay" }
                    };

                    var init = await _paymentService.InitializePaystackPaymentAsync(paystackReq);
                    if (init != null && init.IsSuccess && init.Data?.Data?.AuthorizationUrl != null)
                    {
                        return Ok(new AiChatResponseDto
                        {
                            RequiresSubscription = true,
                            Message = "Subscribe to unlock voice chat, full predictions, and WhatsApp alerts.",
                            SubscribeUrl = init.Data.Data.AuthorizationUrl,
                            AgentDirective = "prompt_payment"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to initialize Paystack checkout for user {UserId}", currentUserId);
                }

                return Ok(new AiChatResponseDto
                {
                    RequiresSubscription = true,
                    Message = "Subscribe to unlock voice chat, full predictions, and WhatsApp alerts.",
                    SubscribeUrl = subscribeUrlFallback,
                    AgentDirective = "prompt_payment"
                });
            }

            string transcript;
            try
            {
                await using var stream = request.Audio.OpenReadStream();
                transcript = await _transcriptionService.TranscribeAsync(stream, request.Audio.FileName, request.Language);
            }
            catch (Exception sttEx)
            {
                _logger.LogError(sttEx, "Transcription failed");
                return StatusCode(500, new { message = "Transcription failed", error = sttEx.Message });
            }

            // Derive tone/scope/context then chat
            var analysis = analysisService.Analyze(transcript);
            var tone = string.IsNullOrWhiteSpace(request.Tone) ? analysis.Tone : request.Tone;
            var scope = string.IsNullOrWhiteSpace(request.Scope) ? analysis.Scope : request.Scope;
            var context = string.IsNullOrWhiteSpace(request.Context) ? analysis.ContextSummary : request.Context;

            try
            {
                var aiText = await _aiChatProvider.GetResponseAsync(currentUserId, transcript, tone, scope, context);

                // Cap AI response to configured maximum characters on voice path as well
                if (!string.IsNullOrEmpty(aiText) && aiText.Length > MaxAiResponseChars)
                {
                    aiText = aiText.Substring(0, MaxAiResponseChars);
                }

                var resp = new AiChatResponseDto { RequiresSubscription = false, Message = aiText, Transcript = transcript };

                if (request.ReturnVoice == true)
                {
                    try
                    {
                        var voice = string.IsNullOrWhiteSpace(request.VoiceStyle) ? _configuration["Voice:DefaultVoice"] ?? "Matthew" : request.VoiceStyle;
                        var s3Prefix = $"aichat/{currentUserId}/{DateTime.UtcNow:yyyyMMdd}";
                        resp.VoiceUrl = await _audioService.SynthesizeToS3Async(aiText, voice, s3Prefix);
                    }
                    catch (Exception ttsEx)
                    {
                        _logger.LogWarning(ttsEx, "TTS failed on voice chat path");
                    }
                }

                return Ok(resp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI chat (voice) failed");

                var isNameResolution =
                    ex.Message?.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) == true ||
                    (ex.InnerException?.Message?.Contains("No such host is known", StringComparison.OrdinalIgnoreCase) == true) ||
                    ex is HttpRequestException;

                if (isNameResolution)
                {
                    return StatusCode(503, new { message = "AI service is temporarily unreachable. Please try again shortly." });
                }

                return StatusCode(500, new { message = "AI chat failed", error = ex.Message });
            }
        }

        private static string BuildUserContent(AiChatRequestDto req)
        {
            var tone = string.IsNullOrWhiteSpace(req.Tone) ? "neutral" : req.Tone.Trim();
            var scope = string.IsNullOrWhiteSpace(req.Scope) ? "football" : req.Scope.Trim();
            var context = string.IsNullOrWhiteSpace(req.Context) ? string.Empty : $"\nContext: {req.Context}";
            return $"User tone: {tone}. Scope: {scope}.{context}\nMessage: {req.Message}";
        }

        public class AiChatRequestDto
        {
            public string? UserId { get; set; }
            public string? Message { get; set; }
            // Optional overrides; if omitted the system derives them from Message
            public string? Tone { get; set; }
            public string? Scope { get; set; }
            public string? Context { get; set; }
            public bool? ReturnVoice { get; set; }
            public string? VoiceStyle { get; set; }
        }

        public class AiVoiceChatRequest
        {
            public string? UserId { get; set; }
            public IFormFile? Audio { get; set; }
            public string? Language { get; set; }
            public string? Tone { get; set; }
            public string? Scope { get; set; }
            public string? Context { get; set; }
            public bool? ReturnVoice { get; set; }
            public string? VoiceStyle { get; set; }
        }

        private async Task LogAiInteractionAsync(string userId, string userMessage, string aiMessage)
        {
            try
            {
                var logsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "logs");
                Directory.CreateDirectory(logsDir);
                var filePath = Path.Combine(logsDir, $"aichat-{DateTime.UtcNow:yyyyMMdd}.log");
                var record = new
                {
                    ts = DateTime.UtcNow,
                    userId,
                    userMessage,
                    aiMessage,
                    lenUser = userMessage?.Length ?? 0,
                    lenAi = aiMessage?.Length ?? 0
                };
                var line = System.Text.Json.JsonSerializer.Serialize(record);
                await System.IO.File.AppendAllTextAsync(filePath, line + Environment.NewLine);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to log AI interaction");
            }
        }

        private async Task<string?> SafeGetStringAsync(string key)
        {
            try
            {
                return await _cache.GetStringAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache get failed for key {Key}; continuing without cache", key);
                return null;
            }
        }

        private async Task SafeSetStringAsync(string key, string value, DistributedCacheEntryOptions options)
        {
            try
            {
                await _cache.SetStringAsync(key, value, options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cache set failed for key {Key}; continuing without cache", key);
            }
        }

        public class AiChatResponseDto
        {
            public bool RequiresSubscription { get; set; }
            public string? Message { get; set; }
            public string? SubscribeUrl { get; set; }
            public string? VoiceUrl { get; set; }
            public string? Transcript { get; set; }
            public string? AgentDirective { get; set; }
            public int? ConversationsRemaining { get; set; }
        }
    }
}

