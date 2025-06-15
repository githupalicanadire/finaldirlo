# Identity Server 4 Mikroservis Entegrasyonu

## 🎯 Entegrasyon Hedefleri

- ✅ **Merkezi Kimlik Doğrulama** - Tek noktadan login/logout
- ✅ **JWT Token Yetkilendirme** - Stateless authentication
- ✅ **API Gateway Güvenliği** - YARP üzerinden secure routing
- ✅ **Kullanıcı Yönetimi** - Kayıt, giriş, profil operations
- ✅ **Mikroservis Güvenliği** - Her API için token validation

## 🏗️ Mimari Genel Bakış

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Shopping.Web  │────│IdentityServer4  │────│   YARP Gateway  │
│   (Client)      │    │   (Port 6010)   │    │   (Port 6004)   │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                                       │
                    ┌──────────────────┬──────────────┼──────────────────┐
                    │                  │              │                  │
            ┌──────▼──────┐    ┌──────▼──────┐ ┌─────▼───���──┐    ┌──────▼──────┐
            │ Catalog API │    │ Basket API  │ │Discount API│    │Ordering API │
            │ (Port 6000) │    │ (Port 6001) │ │(Port 6002) │    │ (Port 6003) │
            └─────────────┘    └─────────────┘ └────────────┘    └─────────────┘
```

## 📁 Proje Yapısı

```
src/
├── Services/
│   ├── Identity/
│   │   └── Identity.API/              # ✅ Identity Server 4 + MongoDB
│   │       ├── Configuration/
│   │       │   ├── Config.cs          # Clients, APIs, Scopes
│   │       │   ├── TestUsers.cs       # Demo kullanıcıları
│   │       │   └── MongoDbSettings.cs # MongoDB ayarları
│   │       ├── Data/
│   │       │   └── MongoIdentityContext.cs
│   │       ├── Models/
│   │       │   ├── ApplicationUser.cs # MongoDB User model
│   │       │   └── AccountViewModels.cs
│   │       ├── Controllers/
│   │       │   ├── AccountController.cs
│   │       │   └── HomeController.cs
│   │       ├── Views/
│   │       │   ├── Account/
│   │       │   ├── Home/
│   │       │   └── Shared/
│   │       ├── appsettings.json
│   │       ��── Dockerfile
│   │       └── Program.cs
│   ├── Catalog/Catalog.API/           # Updated: JWT Auth
│   ├── Basket/Basket.API/             # Updated: JWT Auth
│   ├── Discount/Discount.Grpc/        # Updated: JWT Auth
│   └── Ordering/Ordering.API/         # Updated: JWT Auth
├── ApiGateways/YarpApiGateway/        # Updated: JWT Auth
├── WebApps/Shopping.Web/              # Updated: OIDC Login
├── scripts/
│   └── init-mongo.js                  # ✅ MongoDB initialization
├── docker-compose.yml                 # ✅ Updated: MongoDB + Identity
└── docker-compose.override.yml        # ✅ Updated: Environment vars
```

---

## 🎯 Tamamlanmış Entegrasyon Özellikleri

### ✅ **Identity Server 4 + MongoDB**

- **MongoDB Storage**: AspNetCore.Identity.MongoDbCore ile
- **Collections**: Users, Roles, Clients, ApiResources, PersistedGrants
- **Auto-indexing**: Performance için otomatik index oluşturma
- **TTL Index**: Expired token'lar için otomatik cleanup

### ✅ **Demo Kullanıcıları**

- **admin** / admin123 (Admin Role)
- **customer** / customer123 (Customer Role)
- **demo** / demo123 (User Role)
- **test** / test123 (Tester Role)

### ✅ **Docker Compose Ayarları**

- **MongoDB**: Port 27017, persistent volume
- **Mongo Express**: Port 8081, web admin interface
- **Identity Server**: Port 6010
- **Auto-initialization**: MongoDB collections ve demo data

### ✅ **JWT Token Scopes**

- `catalog.api` - Catalog operations
- `basket.api` - Basket operations
- `ordering.api` - Order operations
- `discount.api` - Discount operations
- `eshop.api` - General API access
- `gateway.api` - API Gateway access

---

## 🚀 Hızlı Başlangıç

### 1. Projeyi Çalıştırma

```bash
# Repository'yi clone edin
git clone [your-repo]
cd [project-folder]

# Docker Compose ile tüm servisleri başlatın
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Servislerin durumunu kontrol edin
docker-compose ps
```

### 2. Erişim Noktaları

- **Identity Server**: http://localhost:6010
- **Shopping Web**: http://localhost:6005
- **API Gateway**: http://localhost:6004
- **MongoDB Admin**: http://localhost:8081 (admin/admin123)
- **RabbitMQ**: http://localhost:15672 (guest/guest)

### 3. Test Senaryosu

1. **Identity Server'a gidin**: http://localhost:6010
2. **Demo kullanıcı ile giriş yapın**: admin/admin123
3. **Shopping Web'e gidin**: http://localhost:6005
4. **Otomatik login olduğunuzu görün**
5. **JWT token ile API'lara erişimi test edin**

---

## 🔧 Mikroservis JWT Entegrasyonu

Her mikroservise JWT authentication eklemek için aşağıdaki adımları izleyin:

### Program.cs'e Authentication Ekleme

```csharp
// JWT Authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = builder.Configuration["IdentitySettings:Authority"];
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["IdentitySettings:Authority"],
            RequireHttpsMetadata = bool.Parse(builder.Configuration["IdentitySettings:RequireHttpsMetadata"] ?? "true")
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiScope", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim("scope", "catalog.api"); // Her servis için farklı scope
    });
});

// Middleware
app.UseAuthentication();
app.UseAuthorization();
```

### Controller'lara Authorization Ekleme

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Tüm endpoint'ler için
public class ProductsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "ApiScope")] // Specific policy
    public async Task<IActionResult> GetProducts()
    {
        // JWT claims'e erişim
        var userId = User.FindFirst("sub")?.Value;
        var userName = User.FindFirst("name")?.Value;
        var userRole = User.FindFirst("role")?.Value;

        // Business logic
    }
}
```

---

## 📊 MongoDB Collections Yapısı

### Users Collection

```json
{
  "_id": ObjectId("..."),
  "UserName": "admin",
  "NormalizedUserName": "ADMIN",
  "Email": "admin@eshop.com",
  "NormalizedEmail": "ADMIN@ESHOP.COM",
  "EmailConfirmed": true,
  "PasswordHash": "...",
  "firstName": "Admin",
  "lastName": "User",
  "address": "123 Admin St",
  "city": "Istanbul",
  "country": "Turkey",
  "createdDate": ISODate("2024-01-01T00:00:00Z"),
  "isActive": true
}
```

### Clients Collection

```json
{
  "_id": ObjectId("..."),
  "ClientId": "shopping.web",
  "ClientName": "Shopping Web App",
  "AllowedGrantTypes": ["authorization_code"],
  "RequirePkce": true,
  "RequireClientSecret": false,
  "RedirectUris": ["http://localhost:6005/signin-oidc"],
  "AllowedScopes": ["openid", "profile", "email", "eshop.api"]
}
```

---

## 🔐 Security Best Practices

### 1. Production Ayarları

```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://username:password@mongodb-cluster/IdentityServerDb",
    "DatabaseName": "IdentityServerDb"
  },
  "IdentitySettings": {
    "RequireHttpsMetadata": true,
    "Authority": "https://identity.yourdomain.com"
  }
}
```

### 2. SSL Certificates

```csharp
// Production için real certificate kullanın
.AddSigningCredential(new X509Certificate2("certificate.pfx", "password"))
```

### 3. CORS Policy

```csharp
services.AddCors(options =>
{
    options.AddPolicy("ProductionCorsPolicy", policy =>
    {
        policy
            .WithOrigins("https://yourdomain.com", "https://api.yourdomain.com")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
```

---

## 🐛 Troubleshooting

### MongoDB Bağlantı Sorunları

```bash
# MongoDB container'ını kontrol edin
docker logs identitydb

# Connection string'i test edin
docker exec -it identitydb mongo --eval "db.adminCommand('ismaster')"
```

### JWT Token Sorunları

```bash
# Identity Server logs
docker logs identity.api

# JWT token'ı decode edin
# https://jwt.io adresinde token'ınızı kontrol edin
```

### Port Çakışmaları

```bash
# Kullanılan portları kontrol edin
netstat -tlnp | grep :6010

# Docker container'ları restart edin
docker-compose restart identity.api
```

Bu entegrasyon ile MongoDB tabanlı, production-ready Identity Server 4 sisteminiz hazır! 🎉

---

## 🔐 1. Identity Server 4 Servisi Oluşturma

### Proje Yapısı

```
src/Services/Identity/Identity.API/
├── Configuration/
│   ├── Config.cs                     # Clients, APIs, Scopes
│   └── TestUsers.cs                  # Test kullanıcıları
├── Data/
│   ├── ApplicationDbContext.cs       # Entity Framework
│   └── Migrations/
├── Models/
│   ├── ApplicationUser.cs            # Identity User
│   └── RegisterViewModel.cs          # Registration model
├── Controllers/
│   ├── AccountController.cs          # Login/Register
│   └── HomeController.cs             # Home page
├── Views/
│   ├── Account/
│   │   ├── Login.cshtml
│   │   ├── Register.cshtml
│   │   └── Logout.cshtml
│   ├── Home/
│   │   └── Index.cshtml
│   └── Shared/
│       └── _Layout.cshtml
├── appsettings.json
├── Dockerfile
├── Identity.API.csproj
└── Program.cs
```
