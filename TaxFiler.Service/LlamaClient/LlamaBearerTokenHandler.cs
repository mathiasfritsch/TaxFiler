using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace TaxFiler.Service.LlamaClient
{
    public class LlamaBearerTokenHandler : DelegatingHandler
    {
        private readonly string _bearerToken;

        public LlamaBearerTokenHandler(IConfiguration configuration)
        {
            var llamaConfig = configuration.GetSection("LlamaParse");
            _bearerToken = llamaConfig["ApiKey"] ?? throw new Exception("LlamaParse:ApiKey not configured");
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _bearerToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}