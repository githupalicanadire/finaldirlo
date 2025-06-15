using Identity.API.Configuration;
using Identity.API.Data;
using Identity.API.Models;
using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using MongoDB.Driver;
using MongoDB.Bson;
using Serilog;
using Microsoft.AspNetCore.Identity;
using IdentityServer4.Stores;

namespace Identity.API;

public class Program
{
    public static void Main(string[] args)
    {
        // Serilog Configuration
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/identity-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            Log.Information("Starting Identity Server with MongoDB...");
            
            // Register MongoDB Class Maps
            MongoDbConfig.RegisterClassMaps();
            
            var host = CreateHostBuilder(args).Build();
            
            // Initialize Database
            SeedDatabase(host);
            
            host.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Identity Server terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });

    private static void SeedDatabase(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;

        try
        {
            var context = services.GetRequiredService<MongoIdentityContext>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Seed Configuration Data
            SeedConfigurationData(context).Wait();

            // Seed Identity Data
            SeedIdentityData(userManager, roleManager).Wait();

            Log.Information("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred seeding the database");
        }
    }

    private static async Task SeedConfigurationData(MongoIdentityContext context)
    {
        // Seed Clients
        var clientsCount = await context.Clients.CountDocumentsAsync(_ => true);
        if (clientsCount == 0)
        {
            var clients = Config.Clients.ToList();
            await context.Clients.InsertManyAsync(clients);
            Log.Information("Clients seeded");
        }

        // Seed Identity Resources
        var identityResourcesCount = await context.IdentityResources.CountDocumentsAsync(_ => true);
        if (identityResourcesCount == 0)
        {
            var identityResources = Config.IdentityResources.ToList();
            await context.IdentityResources.InsertManyAsync(identityResources);
            Log.Information("Identity resources seeded");
        }

        // Seed API Scopes
        var apiScopesCount = await context.ApiScopes.CountDocumentsAsync(_ => true);
        if (apiScopesCount == 0)
        {
            var apiScopes = Config.ApiScopes.ToList();
            await context.ApiScopes.InsertManyAsync(apiScopes);
            Log.Information("API scopes seeded");
        }

        // Seed API Resources
        var apiResourcesCount = await context.ApiResources.CountDocumentsAsync(_ => true);
        if (apiResourcesCount == 0)
        {
            var apiResources = Config.ApiResources.ToList();
            await context.ApiResources.InsertManyAsync(apiResources);
            Log.Information("API resources seeded");
        }
    }

    private static async Task SeedIdentityData(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        // Create Roles
        string[] roles = { "Admin", "Customer", "User", "Tester" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = role, Description = $"{role} role" });
            }
        }

        // Create Users from TestUsers
        foreach (var testUser in TestUsers.Users)
        {
            var user = await userManager.FindByNameAsync(testUser.Username);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = testUser.Username,
                    Email = testUser.Claims.FirstOrDefault(c => c.Type == "email")?.Value,
                    EmailConfirmed = true,
                    FirstName = testUser.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value,
                    LastName = testUser.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value,
                    PhoneNumberConfirmed = true,
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, testUser.Password);
                if (result.Succeeded)
                {
                    // Add claims
                    await userManager.AddClaimsAsync(user, testUser.Claims);
                    
                    // Add to role
                    var roleClaimValue = testUser.Claims.FirstOrDefault(c => c.Type == "role")?.Value;
                    if (!string.IsNullOrEmpty(roleClaimValue))
                    {
                        var roleName = char.ToUpper(roleClaimValue[0]) + roleClaimValue.Substring(1);
                        await userManager.AddToRoleAsync(user, roleName);
                    }
                    
                    Log.Information($"User {testUser.Username} created successfully");
                }
                else
                {
                    Log.Error($"Failed to create user {testUser.Username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        // MongoDB Configuration
        var mongoDbSettings = Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
        services.AddSingleton(mongoDbSettings);

        // MongoDB Client
        services.AddSingleton<IMongoClient>(sp =>
        {
            return new MongoClient(mongoDbSettings.ConnectionString);
        });

        // MongoDB Database
        services.AddSingleton<IMongoDatabase>(sp =>
        {
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(mongoDbSettings.DatabaseName);
        });

        // MongoDB Context
        services.AddSingleton<MongoIdentityContext>();

        // MongoDB Identity Configuration
        var mongoDbIdentityConfig = new MongoDbIdentityConfiguration
        {
            MongoDbSettings = new MongoDbSettings
            {
                ConnectionString = mongoDbSettings.ConnectionString,
                DatabaseName = mongoDbSettings.DatabaseName
            },
            IdentityOptionsAction = options =>
            {
                // Password settings
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                
                // User settings
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false;
            }
        };

        // ASP.NET Core Identity with MongoDB
        services.ConfigureMongoDbIdentity<ApplicationUser, ApplicationRole, ObjectId>(mongoDbIdentityConfig);

        // IdentityServer4 with MongoDB
        services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.EmitStaticAudienceClaim = true;
        })
        .AddInMemoryIdentityResources(Config.IdentityResources)
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryApiResources(Config.ApiResources)
        .AddInMemoryClients(Config.Clients)
        .AddAspNetIdentity<ApplicationUser>()
        .AddDeveloperSigningCredential(); // Only for development

        // Register custom stores
        services.AddTransient<IPersistedGrantStore, MongoPersistedGrantStore>();

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy
                    .WithOrigins(
                        "http://localhost:6005",
                        "https://localhost:6005",
                        "http://localhost:6004",
                        "https://localhost:6004"
                    )
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        // MVC
        services.AddControllersWithViews();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("CorsPolicy");
        
        app.UseIdentityServer();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }
}
