using System.Text.Json;
using FluentResults;
using LlamaParse;
using Microsoft.Extensions.Configuration;
using TaxFiler.DB;
using TaxFiler.Model.Llama;

namespace TaxFiler.Service;

public class ParseService:IParseService
{
    private readonly IConfiguration _configuration;
    private readonly IGoogleDriveService _googleDriveService;
    private readonly TaxFilerContext _taxFilerContext;

    public ParseService(IConfiguration configuration, 
        IGoogleDriveService googleDriveService,
        TaxFilerContext taxFilerContext)
    {
        _configuration = configuration;
        _googleDriveService = googleDriveService;
        _taxFilerContext = taxFilerContext;
    }
    
    public async Task<Result<Invoice>> ParseFilesAsync(int documentId)
    {
        var document = await _taxFilerContext.Documents.FindAsync(documentId);
        
        if(document == null)
        {
            return Result.Fail<Invoice>($"DocumentId {documentId} not found");
        }
        
        var apiKey = _configuration["LlamaParse:ApiKey"];
        
        if(apiKey == null)
        {
            return Result.Fail<Invoice>(" Configuation LlamaParse:ApiKey not found");
        }
        
        var parseConfig = new Configuration
        {
            ApiKey = apiKey,
            StructuredOutput = true,
            StructuredOutputJsonSchemaName = "invoice"
        };
        
        var bytes = await _googleDriveService.DownloadFileAsync(document.ExternalRef);
        
        var client = new LlamaParseClient(new HttpClient(), parseConfig);
        
        var inMemoryFile = new InMemoryFile( new ReadOnlyMemory<byte>(bytes), 
            document.Name.ToLower(),
            FileTypes.GetMimeType(document.Name.ToLower()));
        
        var structuredResults = new List<StructuredResult>();
        await foreach(var structuredResult in client.LoadDataStructuredAsync(inMemoryFile, ResultType.Json))
        {
            structuredResults.Add(structuredResult);
        }
        
        StructuredResult doc = structuredResults.First();
        
        var invoiceStructuredResult = ConvertJsonElementToInvoice(doc.ResultPagesStructured[0]);
        
        document.InvoiceDate = invoiceStructuredResult.InvoiceDate;
        document.InvoiceNumber = invoiceStructuredResult.InvoiceNumber;
        document.Total = invoiceStructuredResult.Total;
        document.SubTotal = invoiceStructuredResult.SubTotal;
        document.TaxAmount = invoiceStructuredResult.Tax.Amount;
        document.TaxRate = invoiceStructuredResult.Tax.Rate;

        document.Parsed = true;
        
        await _taxFilerContext.SaveChangesAsync();
        
        return Result.Ok(invoiceStructuredResult);
    }
    
    private Invoice ConvertJsonElementToInvoice(JsonElement jsonElement)
    {
        var jsonString = jsonElement.GetRawText();
        var invoice = JsonSerializer.Deserialize<Invoice>(jsonString,new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        return invoice;
    }
}