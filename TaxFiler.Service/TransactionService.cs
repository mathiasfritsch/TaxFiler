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
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = ";"
        };
        using var csv = new CsvReader(reader, config);
        var transactions = csv.GetRecords<TransactionDto>();

        return transactions.Select(
            
            t => new TransactionDto
            {
                BookingDate = t.BookingDate,
                SenderReceiver = t.SenderReceiver,
                CounterPartyBIC = t.CounterPartyBIC,
                CounterPartyIBAN = t.CounterPartyIBAN,
                Comment = t.Comment,
                Amount = t.Amount /100
            }
            ).ToArray();
    }

    public async Task<MemoryStream> CreateCsvFileAsync(DateOnly yearMonthh)
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
    

    public async Task AddTransactionsAsync(IEnumerable<TransactionDto> transactions)
    {
        foreach (var transaction in transactions)
        {
            var transactionDb = transaction.ToTransaction();
            transactionDb.IsOutgoing = transactionDb.GrossAmount < 0;
            transactionDb.GrossAmount = Math.Abs(transactionDb.GrossAmount);
            
            taxFilerContext.Transactions.Add(transactionDb);
        }   
        
        await taxFilerContext.SaveChangesAsync();
    }
    

    public async Task DeleteTransactionAsync(int id)
    {
        var transaction = await taxFilerContext.Transactions.FindAsync(id);
        if (transaction != null)
        {
            taxFilerContext.Transactions.Remove(transaction);
            await taxFilerContext.SaveChangesAsync();
        }
    }
    
    
    public async Task<IEnumerable<Model.Dto.TransactionDto>> GetTransactionsAsync(DateOnly yearMonth)
    {
        var transactions = await taxFilerContext
            .Transactions
            .Include(t => t.Document)
            .Where( t => t.TransactionDateTime.Year == yearMonth.Year && t.TransactionDateTime.Month == yearMonth.Month)
            .ToListAsync();
        
        return transactions.Select(t => t.TransactionDto()).ToList();
    }
    
    public async Task<modelDto.TransactionDto> GetTransactionAsync(int transactionId)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == transactionId);
        return transaction.TransactionDto();
    }
    
    public async Task UpdateTransactionAsync(modelDto.UpdateTransactionDto updateTransactionDto)
    {
        var transaction = await taxFilerContext.Transactions.SingleAsync(t => t.Id == updateTransactionDto.Id);
        
        if(transaction.DocumentId != updateTransactionDto.DocumentId && updateTransactionDto.DocumentId > 0)
        {
            var document = await taxFilerContext.Documents.SingleAsync(d => d.Id == updateTransactionDto.DocumentId);
            
            if(document.Skonto is > 0)
            {
                var netAmountSkonto = document.SubTotal.GetValueOrDefault() * (100 - document.Skonto.GetValueOrDefault()) / 100;
                
                updateTransactionDto.NetAmount = Math.Round(netAmountSkonto,2);
                updateTransactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
                updateTransactionDto.TaxAmount = updateTransactionDto.GrossAmount - updateTransactionDto.NetAmount;
            }
            else
            {
                updateTransactionDto.NetAmount = document.SubTotal.GetValueOrDefault();
                updateTransactionDto.TaxAmount = document.TaxAmount.GetValueOrDefault();
                updateTransactionDto.TaxRate = document.TaxRate.GetValueOrDefault();
            }
            
        }
        
        TransactionMapper.UpdateTransaction(transaction, updateTransactionDto);
        await taxFilerContext.SaveChangesAsync();
    }

    public async Task DeleteTransactionsAsync(DateTime yearMonth)
        => await taxFilerContext
            .Transactions
            .Where(t => t.TaxYear == yearMonth.Year && t.TaxMonth == yearMonth.Month)
            .ExecuteDeleteAsync( );
    
}