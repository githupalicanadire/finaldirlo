using Identity.API.Configuration;
using Identity.API.Data;
using Identity.API.Models;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Reflection;

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
            Log.Information("Starting Identity Server...");
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
            // Identity Database
            var identityContext = services.GetRequiredService<ApplicationDbContext>();
            identityContext.Database.Migrate();

            // IdentityServer Configuration Database
            var configContext = services.GetRequiredService<ConfigurationDbContext>();
            configContext.Database.Migrate();

            // IdentityServer Operational Database
            var persistedGrantContext = services.GetRequiredService<PersistedGrantDbContext>();
            persistedGrantContext.Database.Migrate();

            // Seed Configuration Data
            SeedConfigurationData(configContext);

            // Seed Identity Data
            SeedIdentityData(services).Wait();

            Log.Information("Database seeding completed successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred seeding the database");
        }
    }

    private static void SeedConfigurationData(ConfigurationDbContext context)
    {
        if (!context.Clients.Any())
        {
            foreach (var client in Config.Clients)
            {
                context.Clients.Add(client.ToEntity());
            }
            context.SaveChanges();
            Log.Information("Clients seeded");
        }

        if (!context.IdentityResources.Any())
        {
            foreach (var resource in Config.IdentityResources)
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
            context.SaveChanges();
            Log.Information("Identity resources seeded");
        }

        if (!context.ApiScopes.Any())
        {
            foreach (var apiScope in Config.ApiScopes)
            {
                context.ApiScopes.Add(apiScope.ToEntity());
            }
            context.SaveChanges();
            Log.Information("API scopes seeded");
        }

        if (!context.ApiResources.Any())
        {
            foreach (var resource in Config.ApiResources)
            {
                context.ApiResources.Add(resource.ToEntity());
            }
            context.SaveChanges();
            Log.Information("API resources seeded");
        }
    }

    private static async Task SeedIdentityData(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        // Create Roles
        string[] roles = { "Admin", "Customer", "User", "Tester" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
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
                    LastName = testUser.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value
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
        var connectionString = Configuration.GetConnectionString("DefaultConnection");
        var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

        // Entity Framework
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // ASP.NET Identity
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // IdentityServer4
        services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
            options.EmitStaticAudienceClaim = true;
        })
        .AddConfigurationStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(migrationsAssembly));
        })
        .AddOperationalStore(options =>
        {
            options.ConfigureDbContext = b => b.UseSqlServer(connectionString,
                sql => sql.MigrationsAssembly(migrationsAssembly));
        })
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
