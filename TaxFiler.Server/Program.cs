using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaxFiler.DB;
using TaxFiler.Model;
using TaxFiler.Service;
using Microsoft.EntityFrameworkCore;
using Refit;
using TaxFiler.Service.LlamaClient;
using TaxFiler.Service.LlamaIndex;

namespace TaxFiler.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddUserSecrets<Program>();
        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();
        builder.Services.AddDbContext<TaxFilerContext>();
        builder.Services.AddScoped<ISyncService, SyncService>();
        builder.Services.AddScoped<IParseService, ParseService>();
        builder.Services.AddScoped<IGoogleDriveService, GoogleDriveService>();
        builder.Services.AddScoped<IDocumentService, DocumenService>();
        builder.Services.AddScoped<ITransactionService, TransactionService>();
        builder.Services.AddScoped<IAccountService, AccountService>();
        builder.Services.AddScoped<ILlamaIndexService, LlamaIndexService>();
        
        // Document Matching Services
        builder.Services.AddScoped<IDocumentMatchingService, DocumentMatchingService>();
        builder.Services.AddScoped<IAmountMatcher, AmountMatcher>();
        builder.Services.AddScoped<IDateMatcher, DateMatcher>();
        builder.Services.AddScoped<IVendorMatcher, VendorMatcher>();
        builder.Services.AddScoped<IReferenceMatcher, ReferenceMatcher>();
        
        // Document Matching Configuration
        builder.Services.AddSingleton<MatchingConfiguration>(provider =>
        {
            var config = new MatchingConfiguration();
            // Configure default matching configuration
            // These values can be overridden via configuration files or environment variables
            return config;
        });
        builder.Services.Configure<GoogleDriveSettings>(builder.Configuration.GetSection("GoogleDriveSettings"));
        builder.Services.AddTransient<LlamaBearerTokenHandler>();
        builder.Services
            .AddRefitClient<ILlamaApiClient>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri("https://api.cloud.llamaindex.ai");
            })
            .AddHttpMessageHandler<LlamaBearerTokenHandler>();
            
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "EntraId");
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaxFiler API", Version = "v1" });
            c.CustomSchemaIds(x => x.FullName);
            c.CustomOperationIds(apiDesc
                => apiDesc.TryGetMethodInfo(out var methodInfo) ? methodInfo.Name : null);
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "EntraId",
                Name = "oauth2",
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl =
                            new Uri(
                                "https://login.microsoftonline.com/b925eae5-3023-4f8d-8414-8c56b7cee858/oauth2/v2.0/authorize"),
                        TokenUrl = new Uri(
                            "https://login.microsoftonline.com/b925eae5-3023-4f8d-8414-8c56b7cee858/oauth2/v2.0/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            {
                                "api://533594db-31dc-4f88-83e9-b9c0f3a47922/default_access", "Access as User"
                            }
                        }
                    }
                }
            });
            c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("oauth2", doc, null!),
                    new List<string>
                    {
                        "api://533594db-31dc-4f88-83e9-b9c0f3a47922/default_access"
                    }
                }
            });
        });

        var app = builder.Build();
        
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<TaxFilerContext>();
                context.Database.Migrate();
                Console.WriteLine("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred while migrating the database.");
            }
        }

        app.UseDefaultFiles();
        app.UseStaticFiles();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaxFiler API");
                c.OAuthClientId("533594db-31dc-4f88-83e9-b9c0f3a47922");
                c.OAuthUsePkce();
                c.OAuthScopeSeparator(" ");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapFallbackToFile("/index.html");

        app.Run();
    }
}