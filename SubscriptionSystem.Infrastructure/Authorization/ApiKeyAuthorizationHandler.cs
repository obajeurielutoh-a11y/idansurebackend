using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SubscriptionSystem.Application.Interfaces;
using System.Threading.Tasks;

namespace SubscriptionSystem.Infrastructure.Authorization
{
    public class ApiKeyAuthorizationHandler : AuthorizationHandler<ApiKeyRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IApiKeyService _apiKeyService;

        public ApiKeyAuthorizationHandler(IHttpContextAccessor httpContextAccessor, IApiKeyService apiKeyService)
        {
            _httpContextAccessor = httpContextAccessor;
            _apiKeyService = apiKeyService;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var apiKey = httpContext.Request.Headers["X-API-Key"].FirstOrDefault();

            if (apiKey != null && await _apiKeyService.ValidateApiKeyAsync(apiKey))
            {
                context.Succeed(requirement);
            }
        }
    }

    public class ApiKeyRequirement : IAuthorizationRequirement { }
}