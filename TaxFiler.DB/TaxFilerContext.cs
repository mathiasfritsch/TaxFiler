using Microsoft.EntityFrameworkCore;
using TaxFiler.DB.Model;

namespace TaxFiler.DB
{
    public class TaxFilerContext : DbContext
    {
        public TaxFilerContext()
        {
        }

        public TaxFilerContext(DbContextOptions<TaxFilerContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_TaxFilerNeonDB"));
        }
        public DbSet<Document> Documents { get; set; }
        
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Account> Accounts { get; set; }
        
        public DbSet<TransactionDocumentMatcher> TransactionDocumentMatchers { get; set; }
    }
}