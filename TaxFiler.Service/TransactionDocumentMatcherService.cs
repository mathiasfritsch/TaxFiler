using Microsoft.ML;
using Microsoft.ML.Data;
using TaxFiler.Model.Dto;
using TaxFiler.Service.DocumentMatcher;
using TransactionDto = TaxFiler.Model.Csv.TransactionDto;

namespace TaxFiler.Service;
public class TransactionDocumentMatcherService(IDocumentService documentService):ITransactionDocumentMatcherService
{
    public async Task<DocumentDto?> MatchTransactionToDocumentAsync(TransactionDto transaction)
    {
        var mlContext = new MLContext(seed: 42);
        var model = mlContext.Model.Load(@"C:\projects\TaxFiler\TaxFiler.Server\document_transaction_matcher.zip", out _); 
        var featureExtractor = new FeatureExtractor();
        
        var matches = new List<(DocumentModel, TransactionModel, MatchingPrediction)>();

        var documents = await documentService.GetAllUnmatchedDocumentsAsync();
        
        TransactionModel transactionToMatch = new TransactionModel
        {
            GrossAmount = transaction.Amount,
            SenderReceiver = transaction.SenderReceiver,
            Counterparty = transaction.CounterPartyIBAN,
            TransactionNote = transaction.Comment,
            TransactionDateTime = DateTime.SpecifyKind(transaction.BookingDate, DateTimeKind.Utc),
            IsOutgoing = transaction.Amount < 0,
            IsIncomeTaxRelevant = false,
            IsSalesTaxRelevant = false,
        };

        foreach (var document in documents)
        {
            var documentToMatch = new DocumentModel
            {
                Id = document.Id,
                Name =  document.Name,
                ExternalRef = document.ExternalRef,
                Orphaned = document.Orphaned,
                Parsed = document.Parsed,
                InvoiceNumber = document.InvoiceNumber,
                InvoiceDate = document.InvoiceDate.HasValue ? new DateTime(document.InvoiceDate.Value.Year, document.InvoiceDate.Value.Month, document.InvoiceDate.Value.Day) : null,
                SubTotal = document.SubTotal,
                Total = document.Total,
                TaxRate = document.TaxRate,
                TaxAmount = document.TaxAmount,
                Skonto = document.Skonto,
                VendorName = document.VendorName,
                InvoiceDateFromFolder = document.InvoiceDateFromFolder.HasValue ? new DateTime(document.InvoiceDateFromFolder.Value.Year, document.InvoiceDateFromFolder.Value.Month, document.InvoiceDateFromFolder.Value.Day) : null,
            };
            var features = featureExtractor.ExtractFeatures(documentToMatch, transactionToMatch);
            
            var predictionEngine = mlContext.Model.CreatePredictionEngine<MatchingFeatures, MatchingPrediction>(model);
            var prediction = predictionEngine.Predict(features);

            if (prediction.IsMatch)
            {
                return document;
            }
        }
        return null;
    }
}