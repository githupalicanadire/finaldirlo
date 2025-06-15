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
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Wait a bit for MongoDB to be ready
            Task.Delay(5000).Wait();

            // Seed Identity Data
            SeedIdentityData(userManager, roleManager).Wait();

            Log.Information("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred seeding the database");
        }
    }

    private static async Task SeedIdentityData(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        try
        {
            // Create Roles
            string[] roles = { "Admin", "Customer", "User", "Tester" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new ApplicationRole { Name = role, Description = $"{role} role" });
                    Log.Information($"Role {role} created");
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
                        Email = testUser.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? $"{testUser.Username}@eshop.com",
                        EmailConfirmed = true,
                        FirstName = testUser.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? testUser.Username,
                        LastName = testUser.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "User",
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
        catch (Exception ex)
        {
            Log.Error(ex, "Error seeding identity data");
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
        var mongoDbSettings = Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>() ?? new MongoDbSettings();
        if (string.IsNullOrEmpty(mongoDbSettings.ConnectionString))
        {
            mongoDbSettings.ConnectionString = "mongodb://admin:admin123@identitydb:27017/IdentityServerDb?authSource=admin";
        }
        if (string.IsNullOrEmpty(mongoDbSettings.DatabaseName))
        {
            mongoDbSettings.DatabaseName = "IdentityServerDb";
        }
        
        services.AddSingleton(mongoDbSettings);

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
                
                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
            }
        };

        // ASP.NET Core Identity with MongoDB
        services.ConfigureMongoDbIdentity<ApplicationUser, ApplicationRole, ObjectId>(mongoDbIdentityConfig);

        // IdentityServer4
        services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.EmitStaticAudienceClaim = true;
            
            // Set issuer name
            if (!string.IsNullOrEmpty(Configuration["ASPNETCORE_URLS"]))
            {
                options.IssuerUri = Configuration["ASPNETCORE_URLS"];
            }
        })
        .AddInMemoryIdentityResources(Config.IdentityResources)
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryApiResources(Config.ApiResources)
        .AddInMemoryClients(Config.Clients)
        .AddAspNetIdentity<ApplicationUser>()
        .AddDeveloperSigningCredential(); // Only for development

        // CORS
        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", policy =>
            {
                policy
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        // MVC
        services.AddControllersWithViews();
        
        // Health Checks
        services.AddHealthChecks();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
        }

        app.UseStaticFiles();
        app.UseRouting();
        app.UseCors("CorsPolicy");
        
        app.UseIdentityServer();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            endpoints.MapHealthChecks("/health");
        });
    }
}
