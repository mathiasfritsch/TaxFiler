using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaxFiler.DB;
using TaxFiler.Service;
using Shouldly;
namespace TaxFilerTests;


[TestFixture]
public class TransactionServiceTests
{
    private TaxFilerContext _context;
    private TransactionService _transactionService;

    [SetUp]
    public void Setup()
    {
        var inMemorySettings = new Dictionary<string, string> {
            {"ConnectionStrings:TaxFilerDB", "Data Source=C:\\projects\\TaxFiler\\TaxFiler\\TaxfilerDb.db;"}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _context = new TaxFilerContext(configuration);
        _transactionService = new TransactionService(_context);
    }
    
    [TestCase]
    public async Task ParseTransactions()
    {
        var transactionService = new TransactionService(_context); 
        await transactionService.TruncateTransactionsAsync();
        _context.Transactions.Count().ShouldBe(0);
        using var reader = new StreamReader("C:\\projects\\TaxFiler\\TaxFiler\\Finom_statement_25122024.csv");
        var transactions = transactionService.ParseTransactions(reader);
        await transactionService.AddTransactionsAsync(transactions, new DateTime(2024,11,0));
        _context.Transactions.Count().ShouldBe(15);
    }
}