using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace TaxFiler.Service.LlamaClient
{
    public class LlamaBearerTokenHandler : DelegatingHandler
    {
        private const string BearerToken = ""; // Replace with your actual token

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}