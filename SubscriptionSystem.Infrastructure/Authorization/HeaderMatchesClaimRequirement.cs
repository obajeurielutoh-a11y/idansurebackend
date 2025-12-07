using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SubscriptionSystem.Infrastructure.Authorization
{
    public class HeaderMatchesClaimRequirement : IAuthorizationRequirement
    {
        public string HeaderName { get; }
        public string ClaimType { get; }
        public string? ExpectedTokenType { get; }

        public HeaderMatchesClaimRequirement(string headerName, string claimType, string? expectedTokenType = null)
        {
            HeaderName = headerName;
            ClaimType = claimType;
            ExpectedTokenType = expectedTokenType;
        }
    }
}
