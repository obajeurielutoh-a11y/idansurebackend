using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SubscriptionSystem.Application.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;

namespace SubscriptionSystem.Infrastructure.Middleware
{
    public class JwtMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtMiddleware> _logger;

        public JwtMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<JwtMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(
            HttpContext context,
            IJwtService jwtService,
            IApiKeyService apiKeyService
        )
        {
            try
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault();


                if (token != null)
                    await AttachUserToContext(context, token, jwtService);
                else if (apiKey != null)
                    await AttachUserToContextByApiKey(context, apiKey, apiKeyService);

                // Execute downstream middleware and catch any exceptions they throw
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unhandled exception in JwtMiddleware while processing request {Path}", context?.Request?.Path);

                try
                {
                    if (!context.Response.HasStarted)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = 503;
                        context.Response.ContentType = "application/json";
                        var payload = "{\"message\":\"Internal server error\"}";
                        await context.Response.WriteAsync(payload);
                    }
                }
                catch (Exception writeEx)
                {
                    _logger?.LogError(writeEx, "Failed while writing error response in JwtMiddleware for request {Path}", context?.Request?.Path);
                }
            }
        }

        private async Task AttachUserToContext(
            HttpContext context,
            string token,
            IJwtService jwtService
        )
        {
            try
            {
                var principal = jwtService.GetPrincipalFromExpiredToken(token);
                context.User = principal;
            }
            catch
            {
                // Do nothing if token is invalid
            }
        }

        private async Task AttachUserToContextByApiKey(
            HttpContext context,
            string apiKey,
            IApiKeyService apiKeyService
        )
        {
            var user = await apiKeyService.GetUserByApiKeyAsync(apiKey);
            if (user != null)
            {
                var claims = new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                    new Claim(ClaimTypes.Role, user.Role ?? string.Empty),
                    new Claim("ApiKey", apiKey ?? string.Empty)
                };

                var identity = new ClaimsIdentity(claims, "ApiKey");
                context.User = new ClaimsPrincipal(identity);
            }
        }
    }
}