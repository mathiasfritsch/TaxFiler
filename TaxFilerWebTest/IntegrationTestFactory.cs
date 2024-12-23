using System.Data.Common;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaxFiler.DB;
using Testcontainers.MsSql;

namespace TaxFilerWebTest;

public class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private readonly MsSqlContainer _msSqlContainer;
    private readonly DbConnectionFactory _dbConnectionFactory;
    private const string Database = "TaxFilerDB";
    public IntegrationTestFactory()
    {
        var msSqlBuilder = new MsSqlBuilder();
        _msSqlContainer = msSqlBuilder.Build();
        
        _dbConnectionFactory = new DbConnectionFactory(_msSqlContainer, Database);
    }
    

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            var context = services.FirstOrDefault(descriptor => descriptor.ServiceType == typeof(TaxFilerContext));
            if (context != null)
            {
                services.Remove(context);
                var options = services.Where(r => (r.ServiceType == typeof(DbContextOptions))
                                                  || (r.ServiceType.IsGenericType && r.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>))).ToArray();
                foreach (var option in options)
                {
                    services.Remove(option);
                }
            }

            services.AddDbContext<TaxFilerContext>(options =>
                options.UseSqlServer(_dbConnectionFactory.CustomDbConnection.ConnectionString));
            
        });
        builder.UseEnvironment("Development");
    }
    
    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
    }
    public async Task InitializeAsync()
    {
        await _msSqlContainer.StartAsync();

         using var connection = _dbConnectionFactory.MasterDbConnection;
         using var command = connection.CreateCommand();
        
        command.CommandText = "CREATE DATABASE " + Database;

        await connection.OpenAsync()
            .ConfigureAwait(false);

        await command.ExecuteNonQueryAsync()
            .ConfigureAwait(false);
        await connection.CloseAsync();
        
        await using var customConnection = _dbConnectionFactory.CustomDbConnection;
        await customConnection.OpenAsync();
        
        await using var customCommand = customConnection.CreateCommand();       
        
        customCommand.CommandText = @"

create table Documents
(
    Id          int identity
        constraint PK_Documents
            primary key,
    Name        nvarchar(200)                 not null,
    ExternalRef nvarchar(50)                  not null,
    Orphaned    bit default CONVERT([bit], 0) not null
)
;

create table Transactions
(
    Id                   int identity
        constraint PK_Transactions
            primary key,
    NetAmount            decimal(18, 2) not null,
    GrossAmount          decimal(18, 2) not null,
    TaxAmount            decimal(18, 2) not null,
    TaxRate              decimal(18, 2) not null,
    Counterparty         nvarchar(200)  not null,
    TransactionReference nvarchar(200)  not null,
    TransactionDateTime  datetime2      not null,
    TransactionNote      nvarchar(200)  not null
)
;

create table Bookings
(
    Id            int identity
        constraint PK_Bookings
            primary key,
    DocumnentId   int
        constraint FK_Bookings_Documents_DocumnentId
            references Documents,
    TransactionId int
        constraint FK_Bookings_Transactions_TransactionId
            references Transactions
)
;

create index IX_Bookings_DocumnentId
    on Bookings (DocumnentId)
;
create index IX_Bookings_TransactionId
    on Bookings (TransactionId)
;
            ";
        
        customCommand.ExecuteNonQuery();
        
        await customConnection.CloseAsync();
    }

    public new async Task DisposeAsync()
    {
        await _msSqlContainer.DisposeAsync();
    }
    
    private sealed class DbConnectionFactory
    {
        private readonly IDatabaseContainer _databaseContainer;

        private readonly string _database;

        public DbConnectionFactory(IDatabaseContainer databaseContainer, string database)
        {
            _databaseContainer = databaseContainer;
            _database = database;
        }

        public DbConnection MasterDbConnection
        {
            get
            {
                return new SqlConnection(_databaseContainer.GetConnectionString());
            }
        }

        public DbConnection CustomDbConnection
        {
            get
            {
                var connectionString = new SqlConnectionStringBuilder(_databaseContainer.GetConnectionString());
                connectionString.InitialCatalog = _database;
                return new SqlConnection(connectionString.ToString());
            }
        }
    }
}