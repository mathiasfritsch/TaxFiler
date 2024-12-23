using System.Net.Http.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Testcontainers.MsSql;
using TaxFiler;

namespace TaxFilerTests
{
    
    [TestFixture]
    public class HttpTest
    {
        private readonly IContainer _container;
        private  MsSqlContainer _msSqlContainer;
       // private  WebApplicationFactory<Program> _factory;
        
        
        
        public HttpTest()
        {
            _container = new ContainerBuilder()
                .WithImage("testcontainers/helloworld:1.1.0")
                .WithPortBinding(8080, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r.ForPort(8080)))
                .Build();
        }
        
        [TestCase]
        public async Task TestMethod1()
        {
            var httpClient = new HttpClient();
            var requestUri =
                new UriBuilder(
                    Uri.UriSchemeHttp,
                    _container.Hostname,
                    _container.GetMappedPublicPort(8080),
                    "uuid"
                ).Uri;
            var guid = await httpClient.GetStringAsync(requestUri);
        }
        
        [TestCase]
        public async Task TestMethod2()
        {
           // var client = _factory.CreateClient();
           // var actual = await client.GetFromJsonAsync<string>("/home/test");
        }
        
        [OneTimeSetUp]
     public async Task  TestSetup()
     {
         _msSqlContainer = new MsSqlBuilder().Build();
         await _msSqlContainer.StartAsync();
         var connectionString = _msSqlContainer.GetConnectionString();

     
         // _factory = new WebApplicationFactory<Program>()
         //     .WithWebHostBuilder(host => {
         //         host.UseSetting(
         //             "ConnectionStrings:TaxFilerDB", 
         //             connectionString
         //         );
         //     });

         
          await _container.StartAsync();
       
     }

     [OneTimeTearDown]
     public async Task TestTeardown()
     {
        await  _container.DisposeAsync().AsTask();
        await  _msSqlContainer.DisposeAsync().AsTask();
     }
    }
}