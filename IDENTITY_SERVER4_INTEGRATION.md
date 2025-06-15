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
            ┌──────▼──────┐    ┌──────▼──────┐ ┌─────▼──────┐    ┌──────▼──────┐
            │ Catalog API │    │ Basket API  │ │Discount API│    │Ordering API │
            │ (Port 6000) │    │ (Port 6001) │ │(Port 6002) │    │ (Port 6003) │
            └─────────────┘    └─────────────┘ └────────────┘    └─────────────┘
```

## 📁 Proje Yapısı

```
src/
├── Services/
│   ├── Identity/
│   │   └── Identity.API/              # NEW: Identity Server 4
│   ├── Catalog/Catalog.API/
│   ├── Basket/Basket.API/
│   ├── Discount/Discount.Grpc/
│   └── Ordering/Ordering.API/
├── ApiGateways/YarpApiGateway/        # Updated: JWT Auth
├── WebApps/Shopping.Web/              # Updated: Login/Logout
└── docker-compose.yml                 # Updated: Identity service
```

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
