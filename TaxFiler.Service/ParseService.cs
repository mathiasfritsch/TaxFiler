using System.Globalization;
using System.Text.Json;
using FluentResults;
using Microsoft.Extensions.Configuration;
using TaxFiler.DB;
using TaxFiler.Model.Llama;
using TaxFiler.Service.LlamaIndex;

namespace TaxFiler.Service;

public class ParseService : IParseService
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly TaxFilerContext _taxFilerContext;
    private readonly ILlamaIndexService _llamaIndexService;

    public ParseService(
        IGoogleDriveService googleDriveService,
        TaxFilerContext taxFilerContext,
        ILlamaIndexService llamaIndexService)
    {
        _googleDriveService = googleDriveService;
        _taxFilerContext = taxFilerContext;
        _llamaIndexService = llamaIndexService;
    }

    public async Task<Result<LlamaIndexJobResultResponse>> ParseFilesAsync(int documentId)
    {
        var document = await _taxFilerContext.Documents.FindAsync(documentId);

        if (document == null)
        {
            return Result.Fail<LlamaIndexJobResultResponse>($"DocumentId {documentId} not found");
        }
        
        var bytes = await _googleDriveService.DownloadFileAsync(document.ExternalRef);
        
        LlamaIndexJobResultResponse parseResult = await _llamaIndexService.UploadFileAndCreateJobAsync(bytes,  document.Name.ToLower());
       
        var germanCulture = new CultureInfo("de-DE");
        var parsedDateTime = DateTime.TryParse(parseResult.data.InvoiceDate, germanCulture, DateTimeStyles.None, out DateTime parsedDate);
        
        document.InvoiceDate = parsedDateTime ? DateOnly.FromDateTime(parsedDate):null;
        document.InvoiceNumber = parseResult.data.InvoiceNumber;
        document.Total = parseResult.data.Total;
        document.SubTotal = parseResult.data.SubTotal;
        document.TaxAmount = parseResult.data.TaxAmount;
        document.TaxRate = parseResult.data.TaxRate;
        document.Skonto = parseResult.data.Skonto;

        document.Parsed = true;

        await _taxFilerContext.SaveChangesAsync();

        return Result.Ok(parseResult);
    }
    

   
}