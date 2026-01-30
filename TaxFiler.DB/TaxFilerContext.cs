using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TaxFiler.DB.Model;

namespace TaxFiler.DB
{
    public class TaxFilerContext : DbContext
    {
        protected readonly IConfiguration Configuration;
        
        public TaxFilerContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if(Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_TaxFilerNeonDB") !=null)
            {
                options.UseNpgsql(Environment.GetEnvironmentVariable("POSTGRESQLCONNSTR_TaxFilerNeonDB"));
            }
            else
            {
                options.UseNpgsql(Configuration.GetConnectionString("TaxFilerNeonDB"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure composite primary key for TransactionDocument junction table
            modelBuilder.Entity<TransactionDocument>()
                .HasKey(td => new { td.TransactionId, td.DocumentId });
        }

        public DbSet<Document> Documents { get; set; }
        
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Account> Accounts { get; set; }
        
        public DbSet<TransactionDocument> TransactionDocuments { get; set; }
    }
}