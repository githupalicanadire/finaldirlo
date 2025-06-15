using IdentityServer4;
using IdentityServer4.Models;

namespace Identity.API.Configuration;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
        {
            // Mikroservis API Scope'ları
            new ApiScope("catalog.api", "Catalog API"),
            new ApiScope("basket.api", "Basket API"),
            new ApiScope("ordering.api", "Ordering API"),
            new ApiScope("discount.api", "Discount API"),
            new ApiScope("gateway.api", "API Gateway"),
            
            // Genel scope'lar
            new ApiScope("eshop.api", "EShop Microservices API"),
            new ApiScope("read", "Read access"),
            new ApiScope("write", "Write access"),
        };

    public static IEnumerable<ApiResource> ApiResources =>
        new ApiResource[]
        {
            new ApiResource("catalog", "Catalog API")
            {
                Scopes = { "catalog.api", "read", "write" }
            },
            new ApiResource("basket", "Basket API")
            {
                Scopes = { "basket.api", "read", "write" }
            },
            new ApiResource("ordering", "Ordering API")
            {
                Scopes = { "ordering.api", "read", "write" }
            },
            new ApiResource("discount", "Discount API")
            {
                Scopes = { "discount.api", "read", "write" }
            },
            new ApiResource("gateway", "API Gateway")
            {
                Scopes = { "gateway.api", "eshop.api" }
            },
        };

    public static IEnumerable<Client> Clients =>
        new Client[]
        {
            // Shopping Web Application (MVC Client)
            new Client
            {
                ClientId = "shopping.web",
                ClientName = "Shopping Web App",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                
                RedirectUris = 
                { 
                    "http://localhost:6005/signin-oidc",
                    "https://localhost:6005/signin-oidc"
                },
                PostLogoutRedirectUris = 
                { 
                    "http://localhost:6005/signout-callback-oidc",
                    "https://localhost:6005/signout-callback-oidc"
                },
                
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "eshop.api",
                    "catalog.api",
                    "basket.api",
                    "ordering.api",
                    "discount.api"
                },
                
                AccessTokenLifetime = 3600, // 1 hour
                RefreshTokenUsage = TokenUsage.ReUse,
                AllowOfflineAccess = true,
                RequireConsent = false,
                AllowPlainTextPkce = false
            },

            // API Gateway Client (Machine to Machine)
            new Client
            {
                ClientId = "api.gateway",
                ClientName = "API Gateway",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret("gateway_secret".Sha256()) },
                
                AllowedScopes =
                {
                    "eshop.api",
                    "catalog.api",
                    "basket.api",
                    "ordering.api",
                    "discount.api"
                },
                
                AccessTokenLifetime = 3600,
                Claims = 
                {
                    new ClientClaim("role", "gateway"),
                    new ClientClaim("scope", "api_access")
                }
            },

            // Swagger UI Client (Development)
            new Client
            {
                ClientId = "swagger.ui",
                ClientName = "Swagger UI",
                AllowedGrantTypes = GrantTypes.Implicit,
                AllowAccessTokensViaBrowser = true,
                RequireConsent = false,
                
                RedirectUris = 
                { 
                    "http://localhost:6000/swagger/oauth2-redirect.html",
                    "http://localhost:6001/swagger/oauth2-redirect.html",
                    "http://localhost:6003/swagger/oauth2-redirect.html",
                    "http://localhost:6004/swagger/oauth2-redirect.html"
                },
                
                AllowedScopes =
                {
                    "eshop.api",
                    "catalog.api",
                    "basket.api",
                    "ordering.api",
                    "discount.api"
                },
                
                AccessTokenLifetime = 3600
            },

            // Mobile App Client (Future use)
            new Client
            {
                ClientId = "mobile.app",
                ClientName = "Mobile App",
                AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
                ClientSecrets = { new Secret("mobile_secret".Sha256()) },
                
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    "eshop.api",
                    "catalog.api",
                    "basket.api",
                    "ordering.api"
                },
                
                AccessTokenLifetime = 3600,
                RefreshTokenExpiration = TokenExpiration.Sliding,
                SlidingRefreshTokenLifetime = 1296000, // 15 days
                AllowOfflineAccess = true
            },

            // Postman/Testing Client
            new Client
            {
                ClientId = "postman.client",
                ClientName = "Postman Client",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                
                RedirectUris = { "https://www.getpostman.com/oauth2/callback" },
                
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "eshop.api",
                    "catalog.api",
                    "basket.api",
                    "ordering.api",
                    "discount.api"
                },
                
                AccessTokenLifetime = 3600,
                RequireConsent = false
            }
        };
}
