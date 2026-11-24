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
        public DbSet<Document> Documents { get; set; }
        
        public DbSet<Transaction> Transactions { get; set; }

        public DbSet<Account> Accounts { get; set; }

        public DbSet<TransactionDocument> TransactionDocuments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TransactionDocument>(entity =>
            {
                entity.HasOne(td => td.Transaction)
                    .WithMany(t => t.TransactionDocuments)
                    .HasForeignKey(td => td.TransactionId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(td => td.Document)
                    .WithMany(d => d.TransactionDocuments)
                    .HasForeignKey(td => td.DocumentId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Sequence numbers are unique within a transaction's collection.
                entity.HasIndex(td => new { td.TransactionId, td.Sequence })
                    .IsUnique();

                // The FK indexes are generated automatically by convention; the
                // document-side index is added explicitly for the "find transactions
                // for a document" direction of the many-to-many relationship.
                entity.HasIndex(td => td.DocumentId);
            });
        }
    }
}