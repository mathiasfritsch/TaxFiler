using TaxFiler.Service;

namespace TaxFilerTests;


[TestFixture]
public class TransactionServiceTests
{
    [TestCase]
    public void ParseTransactions()
    {
        
        var transactionService = new TransactionService();
        
        transactionService.ParseTransactions();
        
    }
}