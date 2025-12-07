using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
namespace SubscriptionSystem.Infrastructure.Authentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            Microsoft.AspNetCore.Authentication.ISystemClock clock,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _configuration = configuration;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Headers.ContainsKey("Authorization"))
                return AuthenticateResult.Fail("Missing Authorization Header");

            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Context.Request.Headers["Authorization"]);
                if (authHeader.Scheme != "Basic")
                    return AuthenticateResult.Fail("Invalid Authorization Scheme");

                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
                if (credentials.Length != 2)
                    return AuthenticateResult.Fail("Invalid Authorization Header");

                var username = credentials[0];
                var password = credentials[1];

                if (IsAuthorized(username, password))
                {
                    var claims = new[] { new Claim(ClaimTypes.Name, username) };
                    var identity = new ClaimsIdentity(claims, Scheme.Name);
                    var principal = new ClaimsPrincipal(identity);
                    var ticket = new AuthenticationTicket(principal, Scheme.Name);

                    return AuthenticateResult.Success(ticket);
                }

                return AuthenticateResult.Fail("Invalid username or password");
            }
            catch
            {
                return AuthenticateResult.Fail("Invalid Authorization Header");
            }
        }

        private bool IsAuthorized(string username, string password)
        {
            var configUsername = _configuration["BasicAuth:Username"];
            var configPassword = _configuration["BasicAuth:Password"];

            return username == configUsername && password == configPassword;
        }
    }
}

