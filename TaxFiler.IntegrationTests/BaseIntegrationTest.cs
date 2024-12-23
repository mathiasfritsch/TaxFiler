
using Microsoft.Extensions.DependencyInjection;
using TaxFiler.DB;

namespace Application.IntegrationTests;

public abstract class BaseIntegrationTest : IClassFixture<IntegrationTestWebAppFactory>, IDisposable
{
    private readonly IServiceScope _scope;
 
    protected readonly  TaxFilerContext DbContext;
    protected readonly HttpClient Client;
    
    protected BaseIntegrationTest(IntegrationTestWebAppFactory factory)
    {
        _scope = factory.Services.CreateScope();
        Client = factory.CreateClient();
       
        DbContext = _scope.ServiceProvider.GetRequiredService<TaxFilerContext>();
    }

    public void Dispose()
    {
        _scope?.Dispose();
        DbContext?.Dispose();
    }
}