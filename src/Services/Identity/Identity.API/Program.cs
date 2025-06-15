using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using AspNetCore.Identity.MongoDbCore.Extensions;
using AspNetCore.Identity.MongoDbCore.Infrastructure;
using MongoDB.Bson;
using IdentityServer4;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;

namespace Identity.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
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
            // MongoDB Identity Configuration
            var mongoDbIdentityConfig = new MongoDbIdentityConfiguration
            {
                MongoDbSettings = new MongoDbSettings
                {
                    ConnectionString = "mongodb://admin:admin123@identitydb:27017/IdentityServerDb?authSource=admin",
                    DatabaseName = "IdentityServerDb"
                },
                IdentityOptionsAction = options =>
                {
                    options.Password.RequiredLength = 6;
                    options.Password.RequireDigit = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.User.RequireUniqueEmail = true;
                    options.SignIn.RequireConfirmedEmail = false;
                }
            };

            // Configure MongoDB Identity
            services.ConfigureMongoDbIdentity<ApplicationUser, ApplicationRole, ObjectId>(mongoDbIdentityConfig);

            // IdentityServer4
            services.AddIdentityServer()
                .AddInMemoryIdentityResources(GetIdentityResources())
                .AddInMemoryApiScopes(GetApiScopes())
                .AddInMemoryClients(GetClients())
                .AddAspNetIdentity<ApplicationUser>()
                .AddDeveloperSigningCredential();

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
            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
            });
        }

        public static IEnumerable<IdentityServer4.Models.IdentityResource> GetIdentityResources()
        {
            return new IdentityServer4.Models.IdentityResource[]
            {
                new IdentityServer4.Models.IdentityResources.OpenId(),
                new IdentityServer4.Models.IdentityResources.Profile(),
                new IdentityServer4.Models.IdentityResources.Email(),
            };
        }

        public static IEnumerable<IdentityServer4.Models.ApiScope> GetApiScopes()
        {
            return new IdentityServer4.Models.ApiScope[]
            {
                new IdentityServer4.Models.ApiScope("catalog.api", "Catalog API"),
                new IdentityServer4.Models.ApiScope("basket.api", "Basket API"),
                new IdentityServer4.Models.ApiScope("ordering.api", "Ordering API"),
                new IdentityServer4.Models.ApiScope("eshop.api", "EShop API"),
            };
        }

        public static IEnumerable<IdentityServer4.Models.Client> GetClients()
        {
            return new IdentityServer4.Models.Client[]
            {
                new IdentityServer4.Models.Client
                {
                    ClientId = "shopping.web",
                    ClientName = "Shopping Web App",
                    AllowedGrantTypes = IdentityServer4.Models.GrantTypes.Code,
                    RequirePkce = true,
                    RequireClientSecret = false,
                    RedirectUris = { "http://localhost:6005/signin-oidc" },
                    PostLogoutRedirectUris = { "http://localhost:6005/signout-callback-oidc" },
                    AllowedScopes = {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        IdentityServerConstants.StandardScopes.Email,
                        "eshop.api"
                    },
                    RequireConsent = false
                }
            };
        }
    }
}
