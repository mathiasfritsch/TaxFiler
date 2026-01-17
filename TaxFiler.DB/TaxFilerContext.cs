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
    }
}