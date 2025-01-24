using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaxFiler.DB;
using TaxFiler.Model;
using TaxFiler.Service;


namespace TaxFiler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Configuration.AddUserSecrets<Program>();
            builder.Services.AddControllersWithViews()
                .AddMicrosoftIdentityUI();
            builder.Services.AddDbContext<TaxFilerContext>();
            
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
            
            builder.Services.AddScoped<ISyncService,SyncService>();
            builder.Services.AddScoped<IParseService,ParseService>();
            builder.Services.AddScoped<IGoogleDriveService,GoogleDriveService>();
            builder.Services.AddScoped<IDocumentService,DocumenService>();
            builder.Services.AddScoped<ITransactionService,TransactionService>();
            builder.Services.Configure<GoogleDriveSettings>(builder.Configuration.GetSection("GoogleDriveSettings"));
            builder.Services.AddMicrosoftIdentityWebAppAuthentication(builder.Configuration, "EntraId");
            
            var app = builder.Build();
            
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaxFiler API");
                c.OAuthClientId("533594db-31dc-4f88-83e9-b9c0f3a47922");
                c.OAuthUsePkce();
                c.OAuthScopeSeparator(" ");
            });
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    var previousMonth = DateTime.Now.AddMonths(-1);
                    
                    context.Response.Redirect($"/{previousMonth.Year}-{previousMonth.Month}/Home/Index");
                }
                else
                {
                    await next();
                }
            });
            app.MapControllerRoute(
                name: "default",
                pattern: "{yearMonth}/{controller=Home}/{action=Index}/{id?}");
       
            app.UseDefaultFiles();
            
            app.Run();
        }
    }
}