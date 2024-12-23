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
            options.UseSqlite(Configuration.GetConnectionString("TaxFilerDB"));
        }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

    }
}