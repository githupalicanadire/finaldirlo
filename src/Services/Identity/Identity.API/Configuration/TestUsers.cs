using IdentityModel;
using IdentityServer4.Test;
using System.Security.Claims;
using System.Collections.Generic;

namespace Identity.API.Configuration
{
    public static class TestUsers
    {
        public static List<TestUser> Users =>
            new List<TestUser>
            {
                new TestUser
                {
                    SubjectId = "1",
                    Username = "admin",
                    Password = "admin123",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Admin User"),
                        new Claim(JwtClaimTypes.GivenName, "Admin"),
                        new Claim(JwtClaimTypes.FamilyName, "User"),
                        new Claim(JwtClaimTypes.Email, "admin@eshop.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Role, "admin"),
                        new Claim(JwtClaimTypes.WebSite, "http://admin.eshop.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': '123 Admin St', 'locality': 'Istanbul', 'postal_code': 34000, 'country': 'Turkey' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },
                new TestUser
                {
                    SubjectId = "2",
                    Username = "customer",
                    Password = "customer123",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Customer User"),
                        new Claim(JwtClaimTypes.GivenName, "Customer"),
                        new Claim(JwtClaimTypes.FamilyName, "User"),
                        new Claim(JwtClaimTypes.Email, "customer@eshop.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Role, "customer"),
                        new Claim(JwtClaimTypes.WebSite, "http://customer.eshop.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': '456 Customer Ave', 'locality': 'Ankara', 'postal_code': 06000, 'country': 'Turkey' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },
                new TestUser
                {
                    SubjectId = "3",
                    Username = "demo",
                    Password = "demo123",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Demo User"),
                        new Claim(JwtClaimTypes.GivenName, "Demo"),
                        new Claim(JwtClaimTypes.FamilyName, "User"),
                        new Claim(JwtClaimTypes.Email, "demo@eshop.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Role, "user"),
                        new Claim(JwtClaimTypes.WebSite, "http://demo.eshop.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': '789 Demo Blvd', 'locality': 'Izmir', 'postal_code': 35000, 'country': 'Turkey' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                },
                new TestUser
                {
                    SubjectId = "4",
                    Username = "test",
                    Password = "test123",
                    Claims =
                    {
                        new Claim(JwtClaimTypes.Name, "Test User"),
                        new Claim(JwtClaimTypes.GivenName, "Test"),
                        new Claim(JwtClaimTypes.FamilyName, "User"),
                        new Claim(JwtClaimTypes.Email, "test@eshop.com"),
                        new Claim(JwtClaimTypes.EmailVerified, "true", ClaimValueTypes.Boolean),
                        new Claim(JwtClaimTypes.Role, "tester"),
                        new Claim(JwtClaimTypes.WebSite, "http://test.eshop.com"),
                        new Claim(JwtClaimTypes.Address, @"{ 'street_address': '321 Test Rd', 'locality': 'Bursa', 'postal_code': 16000, 'country': 'Turkey' }", IdentityServerConstants.ClaimValueTypes.Json)
                    }
                }
            };
    }
}
