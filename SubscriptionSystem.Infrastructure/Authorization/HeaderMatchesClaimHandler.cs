using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace SubscriptionSystem.Infrastructure.Authorization
{
    public class HeaderMatchesClaimHandler : AuthorizationHandler<HeaderMatchesClaimRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HeaderMatchesClaimHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, HeaderMatchesClaimRequirement requirement)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
            {
                return Task.CompletedTask;
            }

            // Optional token_type check (e.g., "user" vs "admin")
            if (!string.IsNullOrEmpty(requirement.ExpectedTokenType))
            {
                var tokenType = context.User.FindFirst("token_type")?.Value;
                if (!string.Equals(tokenType, requirement.ExpectedTokenType, System.StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }
            }

            // Header present and matches claim
            if (httpContext.Request.Headers.TryGetValue(requirement.HeaderName, out var headerValues))
            {
                var headerVal = headerValues.ToString();
                var claimVal = context.User.FindFirst(requirement.ClaimType)?.Value;
                if (!string.IsNullOrEmpty(headerVal) && !string.IsNullOrEmpty(claimVal) && string.Equals(headerVal, claimVal, System.StringComparison.Ordinal))
                {
                    context.Succeed(requirement);
                }
            }

            return Task.CompletedTask;
        }
    }
}
