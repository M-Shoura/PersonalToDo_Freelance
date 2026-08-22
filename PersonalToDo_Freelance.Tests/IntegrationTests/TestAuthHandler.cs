using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace PersonalToDo_Freelance.Tests.IntegrationTests
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string AuthenticationScheme = "TestScheme";
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Test-User"))
                return Task.FromResult(AuthenticateResult.NoResult());

            var email = Request.Headers["Test-User"].ToString();
            var claims = new[] { new Claim(ClaimTypes.Name, email), new Claim(ClaimTypes.NameIdentifier, email) };
            var identity = new ClaimsIdentity(claims, AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, AuthenticationScheme);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
