namespace LlamaParse;

public class AuthorizationHandler: DelegatingHandler
{
    private readonly string _apiKey;

    public AuthorizationHandler(string apiKey)
    {
        _apiKey = apiKey;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        return base.SendAsync(request, cancellationToken);
    }
}