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
            }
            catch (Exception ex)
            {
                // Log and continue; do not mask downstream pipeline errors
                _logger?.LogError(ex, "JwtMiddleware pre-processing failed for request {Path}", context?.Request?.Path);
            }

            // Always continue the pipeline; let downstream middleware/controllers decide
            await _next(context);
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