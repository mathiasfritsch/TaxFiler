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
        var file = await _taxFilerContext.Documents.FindAsync(documentId);
        
        if(file == null)
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
        
        var bytes = await _googleDriveService.DownloadFileAsync(file.ExternalRef);
        
        var client = new LlamaParseClient(new HttpClient(), parseConfig);
        
        var inMemoryFile = new InMemoryFile( new ReadOnlyMemory<byte>(bytes), 
            file.Name.ToLower(),
            FileTypes.GetMimeType(file.Name.ToLower()));
        
        var documents = new List<StructuredResult>();
        await foreach(var document in client.LoadDataStructuredAsync(inMemoryFile, ResultType.Json))
        {
            documents.Add(document);
        }
        
        StructuredResult doc = documents.First();
        
        var invoice = ConvertJsonElementToInvoice(doc.ResultPagesStructured[0]);
        
        file.InvoiceDate = invoice.InvoiceDate;
        file.InvoiceNumber = invoice.InvoiceNumber;
        file.Total = invoice.Total;
        file.SubTotal = invoice.SubTotal;
        file.TaxAmount = invoice.Tax.Amount;
        file.TaxRate = invoice.Tax.Rate;

        file.Parsed = true;
        
        await _taxFilerContext.SaveChangesAsync();
        
        return Result.Ok(invoice);
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