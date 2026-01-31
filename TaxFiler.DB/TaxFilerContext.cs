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
        
        public DbSet<DocumentAttachment> DocumentAttachments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure many-to-many relationship between Transaction and Document through DocumentAttachment
            modelBuilder.Entity<DocumentAttachment>()
                .HasOne(da => da.Transaction)
                .WithMany(t => t.DocumentAttachments)
                .HasForeignKey(da => da.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<DocumentAttachment>()
                .HasOne(da => da.Document)
                .WithMany(d => d.DocumentAttachments)
                .HasForeignKey(da => da.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Ensure unique constraint on transaction-document pairs to prevent duplicate attachments
            modelBuilder.Entity<DocumentAttachment>()
                .HasIndex(da => new { da.TransactionId, da.DocumentId })
                .IsUnique();

            // Performance optimization indexes for multiple attachments
            // Index for efficient querying of attachments by transaction
            modelBuilder.Entity<DocumentAttachment>()
                .HasIndex(da => da.TransactionId)
                .HasDatabaseName("IX_DocumentAttachments_TransactionId");

            // Index for efficient querying of attachments by document
            modelBuilder.Entity<DocumentAttachment>()
                .HasIndex(da => da.DocumentId)
                .HasDatabaseName("IX_DocumentAttachments_DocumentId");

            // Index for querying automatic vs manual attachments
            modelBuilder.Entity<DocumentAttachment>()
                .HasIndex(da => da.IsAutomatic)
                .HasDatabaseName("IX_DocumentAttachments_IsAutomatic");

            // Index for querying attachments by date range
            modelBuilder.Entity<DocumentAttachment>()
                .HasIndex(da => da.AttachedAt)
                .HasDatabaseName("IX_DocumentAttachments_AttachedAt");

            // Composite index for efficient attachment history queries
            modelBuilder.Entity<DocumentAttachment>()
                .HasIndex(da => new { da.TransactionId, da.AttachedAt })
                .HasDatabaseName("IX_DocumentAttachments_TransactionId_AttachedAt");

            // Index on Transaction fields commonly used in attachment queries
            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.TransactionDateTime)
                .HasDatabaseName("IX_Transactions_TransactionDateTime");

            modelBuilder.Entity<Transaction>()
                .HasIndex(t => t.AccountId)
                .HasDatabaseName("IX_Transactions_AccountId");

            // Index on Document fields commonly used in attachment queries
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.Total)
                .HasDatabaseName("IX_Documents_Total");

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.InvoiceNumber)
                .HasDatabaseName("IX_Documents_InvoiceNumber");

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.InvoiceDate)
                .HasDatabaseName("IX_Documents_InvoiceDate");
        }
    }
}