using InstantAIGate.Admin.Config;
using Microsoft.Extensions.Options;

namespace InstantAIGate.Admin
{
    public class ApiKeyHandler : DelegatingHandler
    {
        private readonly APIClientOptions _options;

        public ApiKeyHandler(IOptions<APIClientOptions> options)
        {
            _options = options.Value;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(_options.AdminApiKey) &&
                !string.Equals(_options.AdminApiKey, "skip", StringComparison.OrdinalIgnoreCase))
            {
                request.Headers.Remove("X-API-Key");
                request.Headers.Add("X-API-Key", _options.AdminApiKey);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
