using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaxFiler.DB.Model;

namespace TaxFiler.DB
{
    public class TaxFilerContext : DbContext
    {
        protected readonly IConfiguration? Configuration;
        
        public TaxFilerContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public TaxFilerContext(DbContextOptions<TaxFilerContext> options) : base(options)
        {
            Configuration = null;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // Skip configuration if options are already set (for testing)
            if (options.IsConfigured)
                return;
                
            if(Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_TaxFilerNeonDB") !=null)
            {
                options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_TaxFilerNeonDB"));
            }
            else if (Configuration != null)
            {
                options.UseNpgsql(Configuration.GetConnectionString("TaxFilerNeonDB"));
            }
        }
        public DbSet<Document> Documents { get; set; }
        
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Account> Accounts { get; set; }
        
        public DbSet<TransactionDocumentMatcher> TransactionDocumentMatchers { get; set; }
    }
}