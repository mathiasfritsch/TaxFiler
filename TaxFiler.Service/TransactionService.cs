using System.Globalization;
using CsvHelper;
using TaxFiler.Model.Csv;

namespace TaxFiler.Service;

public class TransactionService:ITransactionService
{
    public TransactionService()
    {
    }
    

    
    
    public void ParseTransactions()
    {
      
        using (var reader = new StreamReader("C:\\projects\\TaxFiler\\TaxFiler\\Finom_statement_25122024.csv"))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            var records = csv.GetRecords<TransactionDto>();
        }
    }
}