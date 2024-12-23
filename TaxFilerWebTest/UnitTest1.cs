using Microsoft.AspNetCore.Mvc.Testing;
using TaxFilerApi;

namespace TaxFilerWebTest;

[TestFixture]
public class Tests:BaseIntegrationTestClass
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase]
    public async Task Test1()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => { });
        var client = factory.CreateClient();
        
        var response = await client.GetAsync("/test");
    }
}