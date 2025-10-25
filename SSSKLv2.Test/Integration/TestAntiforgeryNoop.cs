using System.Threading.Tasks;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;

namespace SSSKLv2.Test.Integration
{
    // Test-only no-op antiforgery implementation to disable antiforgery validation for IntegrationTest host.
    internal class TestNoopAntiforgery : IAntiforgery
    {
        public AntiforgeryTokenSet GetAndStoreTokens(HttpContext httpContext)
        {
            // Return an empty token set; tests can still add headers if needed.
            return new AntiforgeryTokenSet(string.Empty, "RequestVerificationToken", "__RequestVerificationToken", string.Empty);
        }

        public AntiforgeryTokenSet GetTokens(HttpContext httpContext)
        {
            return new AntiforgeryTokenSet(string.Empty, "RequestVerificationToken", "__RequestVerificationToken", string.Empty);
        }

        public Task<bool> IsRequestValidAsync(HttpContext httpContext)
        {
            // Always consider request valid in tests
            return Task.FromResult(true);
        }

        public Task ValidateRequestAsync(HttpContext httpContext)
        {
            // No-op validation: allow all requests.
            return Task.CompletedTask;
        }

        public void SetCookieTokenAndHeader(HttpContext httpContext)
        {
            // Do nothing for tests
        }
    }
}
