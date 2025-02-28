using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using TaxFiler.DB;
using TaxFiler.Model.Csv;
using modelDto = TaxFiler.Model.Dto;
namespace TaxFiler.Service;

public class TransactionService(TaxFilerContext taxFilerContext):ITransactionService
{
    public IEnumerable<TransactionDto> ParseTransactions(TextReader reader)
    {
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var transactions = csv.GetRecords<TransactionDto>();
        return transactions.ToArray();
    }

    public async Task<MemoryStream> CreateCsvFileAsync(DateTime yearMonthh)
    {
        var transactions = await taxFilerContext
            .Transactions.Include(t => t.Document)
            .Where(t => t.TaxYear == yearMonthh.Year && t.TaxMonth == yearMonthh.Month)
            .Where(t => t.IsSalesTaxRelevant || t.IsIncomeTaxRelevant)
            .OrderBy(t => t.TransactionDateTime)
            .ToListAsync();

        var transactionReportDtos = transactions.Select
            (
                t => new TranactionReportDto
                {
                    NetAmount = t.NetAmount,
                    GrossAmount = t.GrossAmount,
                    TaxAmount = t.TaxAmount,
                    TaxRate = t.TaxRate,
                    TransactionReference = t.TransactionReference,
                    TransactionDateTime = t.TransactionDateTime,
                    TransactionNote = t.TransactionNote,
                    IsOutgoing = t.IsOutgoing,
                    IsIncomeTaxRelevant = t.IsIncomeTaxRelevant,
                    IsSalesTaxRelevant = t.IsSalesTaxRelevant,
                    DocumentName = t.IsOutgoing? $"Rechnungseingang/{t.Document?.Name}":$"Rechnungsausgang/{t.Document?.Name}",
                    SenderReceiver = t.SenderReceiver
                }
            ).ToList();
        
        var memoryStream = new MemoryStream();

        await using var writer = new StreamWriter(memoryStream, leaveOpen: true);
        
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ",",
            Encoding = Encoding.UTF8 
        };

        await using var csv = new CsvWriter(writer, config) ;
        
        await csv.WriteRecordsAsync(transactionReportDtos); 
        await writer.FlushAsync();
        
        return memoryStream;
    }
    

    public async Task AddTransactionsAsync(IEnumerable<TransactionDto> transactions, DateTime yearMonth)
    {
        foreach (var transaction in transactions)
        {
            var transactionDb = transaction.ToTransaction();
            transactionDb.TaxYear = yearMonth.Year;
            transactionDb.TaxMonth = yearMonth.Month;
            transactionDb.IsOutgoing = transactionDb.GrossAmount < 0;
            transactionDb.GrossAmount = Math.Abs(transactionDb.GrossAmount);
            
            taxFilerContext.Transactions.Add(transactionDb);
        }   
        
        await taxFilerContext.SaveChangesAsync();
    }
    
    public async Task TruncateTransactionsAsync()
        => await taxFilerContext.Transactions.ExecuteDeleteAsync( );

    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync()
    {
        var transactions = await taxFilerContext.Transactions.Include( t => t.Document).ToListAsync();
        return transactions.Select(t => t.TransactionDto()).ToList();
    }
    
    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync(DateTime yearMonth)
    {
        var transactions = await taxFilerContext
            .Transactions
            .Include(t => t.Document)
            .Where( t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ToListAsync();
        
        return transactions.Select(t => t.TransactionDto()).ToList();
    }
    
    public async Task<modelDto.TransactionDto> GetTransactionAsync(int transactionId)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionId);
        return transaction.TransactionDto();
    }
    
    public async Task UpdateTransactionAsync(modelDto.TransactionDto transactionDto)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionDto.Id);
        
        if(transaction.DocumentId != transactionDto.DocumentId && transactionDto.DocumentId > 0)
        {
            var document = await taxFilerContext.Documents.SingleAsync(d => d.Id == transactionDto.DocumentId);
            
            if(document.Skonto is > 0)
            {
                var netAmountSkonto = document.SubTotal.GetValueOrDefault() * (100 - document.Skonto.GetValueOrDefault()) / 100;
                
                transactionDto.NetAmount = Math.Round(netAmountSkonto,2);
                transactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
                transactionDto.TaxAmount = transactionDto.GrossAmount - transactionDto.NetAmount;
            }
            else
            {
                transactionDto.NetAmount = document.SubTotal.GetValueOrDefault();
                transactionDto.TaxAmount = document.TaxAmount.GetValueOrDefault();
                transactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
            }
            
        }
        
        TransactionMapper.UpdateTransaction(transaction, transactionDto);
        await taxFilerContext.SaveChangesAsync();
    }

    public async Task DeleteTransactionsAsync(DateTime yearMonth)
        => await taxFilerContext
            .Transactions
            .Where(t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ExecuteDeleteAsync( );
    
}