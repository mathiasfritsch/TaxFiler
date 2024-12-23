using TaxFiler.DB.Model;

namespace Application.IntegrationTests;

public class DocumentsTests : BaseIntegrationTest
{
    public DocumentsTests(IntegrationTestWebAppFactory factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task DocumentsTest()
    {
        var id = await CreateDocumentAsync();
   
        var result = await Client.GetAsync("home/documents");

        Assert.NotNull(result);
    }
    
    private async Task<int> CreateDocumentAsync()
    {
        var document = new Document
        {
            ExternalRef = "externalRef",
            Name = "name",
            Orphaned = false
        };
        DbContext.Documents.Add(document);
        var result = await DbContext.SaveChangesAsync();
        
        return document.Id;
    }
}