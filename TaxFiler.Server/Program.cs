using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaxFiler.DB;
using TaxFiler.Model;
using TaxFiler.Service;

namespace TaxFiler.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddUserSecrets<Program>();
        // Add services to the container.
        builder.Services.AddControllersWithViews()
            .AddMicrosoftIdentityUI();
        builder.Services.AddDbContext<TaxFilerContext>();
        builder.Services.AddScoped<ISyncService,SyncService>();
        builder.Services.AddScoped<IParseService,ParseService>();
        builder.Services.AddScoped<IGoogleDriveService,GoogleDriveService>();
        builder.Services.AddScoped<IDocumentService,DocumenService>();
        builder.Services.AddScoped<ITransactionService,TransactionService>();
        builder.Services.Configure<GoogleDriveSettings>(builder.Configuration.GetSection("GoogleDriveSettings"));

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
                            AuthorizationUrl = new Uri("https://login.microsoftonline.com/b925eae5-3023-4f8d-8414-8c56b7cee858/oauth2/v2.0/authorize"),
                            TokenUrl = new Uri("https://login.microsoftonline.com/b925eae5-3023-4f8d-8414-8c56b7cee858/oauth2/v2.0/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                {
                                    "api://533594db-31dc-4f88-83e9-b9c0f3a47922/default_access", "Access as User"
                                }
                            }
                        }
                    }
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new()
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "oauth2"
                            }
                        },
                        new[]
                        {
                            "api://533594db-31dc-4f88-83e9-b9c0f3a47922/default_access"
                        }
                    }
                });
            });
                    
        var app = builder.Build();

        app.UseDefaultFiles();
        app.UseStaticFiles();

        // Configure the HTTP request pipeline.
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
