using Microsoft.Extensions.DependencyInjection;
using TaxFiler.DB;

namespace TaxFilerWebTest;
using TaxFilerApi;

[TestFixture]
public abstract class BaseIntegrationTestClass()
{
    public IntegrationTestFactory<Program> Factory { get; } = new();
    public IServiceScope ServiceScope { get; private set; }
    public TaxFilerContext TaxFilerContext { get; private set; }
    
    [OneTimeSetUp]
    public async Task Init()
    {
        await Factory.InitializeAsync();
        ServiceScope = Factory.Services.CreateScope();
        TaxFilerContext = ServiceScope.ServiceProvider.GetService<TaxFilerContext>()!;
    }
    
    [TearDown]
    public void CleanupTest()
    {
        
    }
    
    [OneTimeTearDown]
    public async Task Cleanup()
    {
        await Factory.DisposeAsync();
        if (TaxFilerContext != null) await TaxFilerContext.DisposeAsync();
        ServiceScope?.Dispose();
    }
}