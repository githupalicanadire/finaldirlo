# .NET 8 Mikroservis E-Ticaret Projesi - Detaylı Eğitim Rehberi

## 📋 İçindekiler

1. [Genel Mimari ve Konseptler](#1-genel-mimari-ve-konseptler)
2. [Kullanılan Teknolojiler](#2-kullanılan-teknolojiler)
3. [Mikroservisler Detayı](#3-mikroservisler-detayı)
4. [API Gateway (YARP)](#4-api-gateway-yarp)
5. [Web UI (Shopping.Web)](#5-web-ui-shoppingweb)
6. [Veritabanları ve Mesajlaşma](#6-veritabanları-ve-mesajlaşma)
7. [Clean Architecture & DDD](#7-clean-architecture--ddd)
8. [CQRS ve MediatR](#8-cqrs-ve-mediatr)
9. [Docker ve Konteynerizasyon](#9-docker-ve-konteynerizasyon)
10. [Kod Örnekleri ve İmplementasyon](#10-kod-örnekleri-ve-implementasyon)

---

## 1. Genel Mimari ve Konseptler

### 🏗️ Mikroservis Mimarisi Nedir?

**Mikroservis mimarisi**, büyük bir uygulamayı küçük, bağımsız, tek bir iş sorumluluğuna sahip servisler halinde bölen bir mimari yaklaşımdır.

#### Bu Projede Mikroservis Avantajları:

- **Bağımsız Geliştirme**: Her servis ayrı takım tarafından geliştirilebilir
- **Teknoloji Çeşitliliği**: Her servis farklı teknoloji kullanabilir
- **Ölçeklenebilirlik**: Sadece ihtiyaç olan servis ölçeklenir
- **Hata İzolasyonu**: Bir serviste hata diğerlerini etkilemez

### 🔄 Proje Mimarisi

```
┌─────────────────┐    ┌─────────────────┐
│   Shopping.Web  │────│  YARP Gateway   │
│   (Frontend)    │    │   (Port 6004)   │
└─────────────────┘    └─────────────────┘
                               │
            ┌──��───────────────┼──────────────────┐
            │                  │                  │
    ┌──────▼──────┐    ┌──────▼──────┐    ┌──────▼──────┐
    │ Catalog API │    │ Basket API  │    │Ordering API │
    │ (Port 6000) │    │ (Port 6001) │    │ (Port 6003) │
    └─────────────┘    └─────────────┘    └─────────────┘
                               │
                       ┌──────▼──────┐
                       │Discount gRPC│
                       │ (Port 6002) │
                       └─────────────┘
```

---

## 2. Kullanılan Teknolojiler

### 🔧 Backend Teknolojileri

- **.NET 8**: En güncel .NET framework
- **ASP.NET Core**: Web API geliştirme
- **Entity Framework Core**: ORM (Object-Relational Mapping)
- **MediatR**: CQRS pattern implementasyonu
- **FluentValidation**: Validasyon kütüphanesi
- **Carter**: Minimal API endpoint tanımlaması
- **Marten**: PostgreSQL için document database
- **gRPC**: Yüksek performanslı inter-service iletişim

### 🗄️ Veritabanları

- **PostgreSQL**: Catalog ve Basket servisleri için
- **SQL Server**: Ordering servisi için
- **SQLite**: Discount servisi için
- **Redis**: Dağıtılmış cache

### 📡 Mesajlaşma ve İletişim

- **RabbitMQ**: Asenkron mesajlaşma
- **MassTransit**: Message broker abstraction
- **YARP**: Reverse proxy (API Gateway)

### 🐳 DevOps ve Konteynerizasyon

- **Docker**: Konteynerizasyon
- **Docker Compose**: Multi-container orchestration

---

## 3. Mikroservisler Detayı

### 📦 Catalog API (Port 6000)

**Sorumluluk**: Ürün katalog yönetimi

#### Teknoloji Stack:

```csharp
// appsettings.json
{
  "ConnectionStrings": {
    "Database": "Server=catalogdb;Port=5432;Database=CatalogDb;..."
  }
}
```

#### Özellikler:

- **Vertical Slice Architecture**: Her feature kendi klasöründe
- **Marten**: PostgreSQL üzerinde document database
- **Carter**: Minimal API endpoints
- **MediatR**: CQRS pattern

#### Endpoints:

- `GET /products` - Tüm ürünleri listele
- `GET /products/{id}` - Ürün detayı
- `GET /products/category/{category}` - Kategoriye göre ürünler
- `POST /products` - Yeni ürün oluştur
- `PUT /products` - Ürün güncelle
- `DELETE /products/{id}` - Ürün sil

#### Kod Yapısı:

```
Catalog.API/
├── Products/
│   ├── GetProducts/
│   │   ├── GetProductsEndpoint.cs
│   │   └── GetProductsHandler.cs
│   ├── CreateProduct/
│   └── UpdateProduct/
├── Models/
│   └── Product.cs
└── Program.cs
```

### 🛒 Basket API (Port 6001)

**Sorumluluk**: Sepet yönetimi ve Redis cache

#### Özellikler:

- **Redis Cache**: Performanslı sepet saklama
- **gRPC İletişim**: Discount servisinden indirim alma
- **Event Publishing**: RabbitMQ ile sipariş eventi yayınlama

#### Cache Pattern:

- **Cache-Aside Pattern**: Veri önce cache'den okunur
- **Decorator Pattern**: Cache işlemleri için
- **Repository Pattern**: Veri erişim katmanı

#### Kod Örneği:

```csharp
public class BasketRepository : IBasketRepository
{
    private readonly IDatabase _database;

    public async Task<ShoppingCart> GetBasket(string userName)
    {
        var basket = await _database.StringGetAsync(userName);
        return string.IsNullOrEmpty(basket)
            ? null
            : JsonSerializer.Deserialize<ShoppingCart>(basket);
    }
}
```

### 💰 Discount gRPC (Port 6002)

**Sorumluluk**: İndirim hesaplama servisi

#### gRPC Nedir?

- **Yüksek Performans**: Binary serileştirme
- **Type-Safe**: Strongly typed contracts
- **Cross-Platform**: Farklı dillerde çalışabilir

#### Protocol Buffer (.proto dosyası):

```protobuf
syntax = "proto3";

option csharp_namespace = "Discount.Grpc";

service DiscountProtoService {
    rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
}

message GetDiscountRequest {
    string productName = 1;
}

message CouponModel {
    int32 id = 1;
    string productName = 2;
    string description = 3;
    int32 amount = 4;
}
```

### 📋 Ordering API (Port 6003)

**Sorumluluk**: Sipariş işleme (En kompleks servis)

#### Mimari Katmanları:

```
Ordering.Domain/          # Domain Layer (Entities, Value Objects)
├── Models/
│   ├── Order.cs
│   ├── OrderItem.cs
│   └── Customer.cs
├── ValueObjects/
│   ├── Address.cs
│   ├── Payment.cs
│   └── OrderId.cs
└── Events/
    ├── OrderCreatedEvent.cs
    └── OrderUpdatedEvent.cs

Ordering.Application/     # Application Layer (Use Cases)
├── Orders/
│   ├── Commands/
│   ├── Queries/
│   └── EventHandlers/
└── Dtos/

Ordering.Infrastructure/  # Infrastructure Layer (Data Access)
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Configurations/
└── Migrations/

Ordering.API/            # Presentation Layer (Controllers)
└── Endpoints/
```

#### DDD (Domain-Driven Design) Konseptleri:

**Aggregate Root:**

```csharp
public class Order : Aggregate<OrderId>
{
    public CustomerId CustomerId { get; private set; }
    public OrderName OrderName { get; private set; }
    public Address ShippingAddress { get; private set; }
    private readonly List<OrderItem> _orderItems = new();

    public static Order Create(OrderId id, CustomerId customerId, ...)
    {
        var order = new Order { Id = id, CustomerId = customerId };
        order.AddDomainEvent(new OrderCreatedEvent(order));
        return order;
    }
}
```

**Value Object Örneği:**

```csharp
public record Address
{
    public string FirstName { get; init; }
    public string LastName { get; init; }
    public string EmailAddress { get; init; }
    public string AddressLine { get; init; }
    public string Country { get; init; }
    public string State { get; init; }
    public string ZipCode { get; init; }
}
```

---

## 4. API Gateway (YARP)

### 🚪 YARP Nedir?

**YARP (Yet Another Reverse Proxy)**, Microsoft tarafından geliştirilen yüksek performanslı reverse proxy kütüphanesidir.

#### Faydaları:

- **Tek Giriş Noktası**: Tüm mikroservisler için
- **Load Balancing**: Yük dağılımı
- **Rate Limiting**: İstek sınırlama
- **Authentication**: Kimlik doğrulama
- **Request/Response Transformation**: İstek dönüştürme

#### YARP Konfigürasyonu:

```json
{
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/catalog-service/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "/{**catch-all}" }]
      },
      "basket-route": {
        "ClusterId": "basket-cluster",
        "Match": {
          "Path": "/basket-service/{**catch-all}"
        }
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://catalog.api:8080/"
          }
        }
      },
      "basket-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://basket.api:8080/"
          }
        }
      }
    }
  }
}
```

#### Rate Limiting Örneği:

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromSeconds(10);
        opt.PermitLimit = 5;
    });
});
```

---

## 5. Web UI (Shopping.Web)

### 🖥️ ASP.NET Core Razor Pages

**Shopping.Web**, ASP.NET Core Razor Pages kullanarak geliştirilmiş bir e-ticaret frontend'idir.

#### Mimari:

```
Shopping.Web/
├── Pages/
│   ├── Index.cshtml              # Ana sayfa
│   ├── ProductList.cshtml        # Ürün listesi
│   ├── ProductDetail.cshtml      # Ürün detayı
│   ├── Cart.cshtml               # Sepet
│   └── Checkout.cshtml           # Ödeme
├── Models/
│   ├── Catalog/ProductModel.cs
│   ├── Basket/ShoppingCartModel.cs
│   └── Ordering/OrderModel.cs
├── Services/
│   ├── ICatalogService.cs
│   ├── IBasketService.cs
│   └── IOrderingService.cs
└── wwwroot/
    ├── css/style.css
    └── lib/bootstrap/
```

#### Refit HTTP Client:

```csharp
[Headers("Content-Type: application/json")]
public interface ICatalogService
{
    [Get("/catalog-service/products")]
    Task<IEnumerable<ProductModel>> GetProducts();

    [Get("/catalog-service/products/{id}")]
    Task<ProductModel> GetProduct(Guid id);

    [Get("/catalog-service/products/category/{category}")]
    Task<IEnumerable<ProductModel>> GetProductByCategory(string category);
}
```

#### Dependency Injection:

```csharp
builder.Services.AddRefitClient<ICatalogService>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
    });
```

#### Razor Page Örneği:

```csharp
public class IndexModel : PageModel
{
    private readonly ICatalogService _catalogService;

    public IndexModel(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    public IEnumerable<ProductModel> ProductList { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync()
    {
        ProductList = await _catalogService.GetProducts();
        return Page();
    }
}
```

---

## 6. Veritabanları ve Mesajlaşma

### 🗄️ Veritabası Stratejileri

#### Database per Microservice Pattern:

Her mikroservisin kendi veritabanı vardır:

- **Catalog**: PostgreSQL (Document-based with Marten)
- **Basket**: PostgreSQL + Redis Cache
- **Ordering**: SQL Server (Relational)
- **Discount**: SQLite (File-based)

#### Marten (Document Database):

```csharp
// Product document olarak saklanır
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }
    public string Description { get; set; }
    public string ImageFile { get; set; }
    public decimal Price { get; set; }
}

// Program.cs
builder.Services.AddMarten(options =>
{
    options.Connection(connectionString);
    options.Schema.For<Product>();
}).UseLightweightSessions();
```

### 📡 Event-Driven Architecture

#### RabbitMQ ile Asenkron İletişim:

**Event Publishing (Basket → Ordering):**

```csharp
public class BasketCheckoutEvent : IntegrationEvent
{
    public string UserName { get; set; }
    public decimal TotalPrice { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    // Diğer checkout bilgileri...
}

// Basket.API
public async Task<CheckoutBasketResult> Handle(CheckoutBasketCommand command)
{
    // Sepet al
    var basket = await repository.GetBasket(command.BasketCheckoutDto.UserName);

    // Event oluştur
    var eventMessage = command.BasketCheckoutDto.Adapt<BasketCheckoutEvent>();

    // Event publish et
    await publishEndpoint.Publish(eventMessage);

    // Sepeti sil
    await repository.DeleteBasket(command.BasketCheckoutDto.UserName);
}
```

**Event Consuming (Ordering):**

```csharp
public class BasketCheckoutEventHandler : IConsumer<BasketCheckoutEvent>
{
    public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
    {
        var command = new CreateOrderCommand(context.Message.Adapt<OrderDto>());
        await mediator.Send(command);
    }
}
```

---

## 7. Clean Architecture & DDD

### ���️ Clean Architecture Katmanları

#### Ordering Mikroservisi Örneği:

**Domain Layer (En içteki katman):**

```csharp
// Entity Base Class
public abstract class Entity<T> : IEntity<T>
{
    public T Id { get; set; }
    public DateTime? CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModified { get; set; }
    public string? LastModifiedBy { get; set; }
}

// Aggregate Root
public abstract class Aggregate<TId> : Entity<TId>, IAggregate<TId>
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}
```

**Application Layer:**

```csharp
// Use Case (Command Handler)
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand, CreateOrderResult>
{
    public async Task<CreateOrderResult> Handle(CreateOrderCommand command)
    {
        // Business Logic
        var order = CreateNewOrder(command.Order);

        // Save to database
        context.Orders.Add(order);
        await context.SaveChangesAsync();

        return new CreateOrderResult(order.Id.Value);
    }

    private Order CreateNewOrder(OrderDto orderDto)
    {
        // Domain object oluşturma
        var shippingAddress = Address.Of(orderDto.ShippingAddress.FirstName, ...);
        var billingAddress = Address.Of(orderDto.BillingAddress.FirstName, ...);
        var payment = Payment.Of(orderDto.Payment.CardName, ...);

        var order = Order.Create(
            OrderId.Of(Guid.NewGuid()),
            CustomerId.Of(orderDto.CustomerId),
            OrderName.Of(orderDto.OrderName),
            shippingAddress,
            billingAddress,
            payment);

        return order;
    }
}
```

**Infrastructure Layer:**

```csharp
// EF Core Configuration
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasConversion(
            orderId => orderId.Value,
            dbId => OrderId.Of(dbId));

        builder.ComplexProperty(o => o.OrderName, nameBuilder =>
        {
            nameBuilder.Property(n => n.Value)
                .HasColumnName(nameof(Order.OrderName))
                .HasMaxLength(100)
                .IsRequired();
        });
    }
}
```

---

## 8. CQRS ve MediatR

### 🔄 CQRS (Command Query Responsibility Segregation)

CQRS, okuma ve yazma işlemlerini ayıran bir pattern'dir.

#### Command (Yazma İşlemleri):

```csharp
// Command
public record CreateProductCommand(ProductDto Product) : ICommand<CreateProductResult>;
public record CreateProductResult(Guid Id);

// Command Handler
public class CreateProductHandler : ICommandHandler<CreateProductCommand, CreateProductResult>
{
    public async Task<CreateProductResult> Handle(CreateProductCommand command)
    {
        // Business logic
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = command.Product.Name,
            Category = command.Product.Category,
            Description = command.Product.Description,
            ImageFile = command.Product.ImageFile,
            Price = command.Product.Price
        };

        // Save to database
        session.Store(product);
        await session.SaveChangesAsync();

        return new CreateProductResult(product.Id);
    }
}
```

#### Query (Okuma İşlemleri):

```csharp
// Query
public record GetProductsQuery(int? PageNumber = 1, int? PageSize = 10) : IQuery<GetProductsResult>;
public record GetProductsResult(IEnumerable<Product> Products);

// Query Handler
public class GetProductsHandler : IQueryHandler<GetProductsQuery, GetProductsResult>
{
    public async Task<GetProductsResult> Handle(GetProductsQuery query)
    {
        var products = await session.Query<Product>()
            .Skip((query.PageNumber.Value - 1) * query.PageSize.Value)
            .Take(query.PageSize.Value)
            .ToListAsync();

        return new GetProductsResult(products);
    }
}
```

#### MediatR Pipeline Behaviors:

**Validation Behavior:**

```csharp
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context)));

        var failures = validationResults
            .Where(r => r.Errors.Any())
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Any())
            throw new ValidationException(failures);

        return await next();
    }
}
```

**Logging Behavior:**

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next)
    {
        logger.LogInformation(
            "[START] Handle request={Request} - Response={Response} - RequestData={RequestData}",
            typeof(TRequest).Name, typeof(TResponse).Name, request);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        logger.LogInformation(
            "[END] Handled {Request} with {Response} in {ElapsedMilliseconds} ms",
            typeof(TRequest).Name, typeof(TResponse).Name, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
```

---

## 9. Docker ve Konteynerizasyon

### 🐳 Docker Compose Yapılandırması

#### docker-compose.yml:

```yaml
version: "3.4"

services:
  # Databases
  catalogdb:
    image: postgres
    container_name: catalogdb
    environment:
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres
      - POSTGRES_DB=CatalogDb
    ports:
      - "5432:5432"
    volumes:
      - postgres_catalog:/var/lib/postgresql/data/

  # Cache
  distributedcache:
    image: redis
    container_name: distributedcache
    ports:
      - "6379:6379"

  # Message Broker
  messagebroker:
    image: rabbitmq:management
    container_name: messagebroker
    environment:
      - RABBITMQ_DEFAULT_USER=guest
      - RABBITMQ_DEFAULT_PASS=guest
    ports:
      - "5672:5672"
      - "15672:15672"

  # Microservices
  catalog.api:
    image: catalogapi
    build:
      context: .
      dockerfile: Services/Catalog/Catalog.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__Database=Server=catalogdb;Port=5432;Database=CatalogDb;User Id=postgres;Password=postgres
    depends_on:
      - catalogdb
    ports:
      - "6000:8080"
```

#### Dockerfile Örneği (Catalog.API):

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Catalog/Catalog.API/Catalog.API.csproj", "Services/Catalog/Catalog.API/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]

RUN dotnet restore "./Services/Catalog/Catalog.API/Catalog.API.csproj"
COPY . .
WORKDIR "/src/Services/Catalog/Catalog.API"
RUN dotnet build "./Catalog.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Catalog.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

---

## 10. Kod Örnekleri ve İmplementasyon

### 📝 Pratik Örnekler

#### 1. Catalog API - Product CRUD İşlemleri

**Endpoint Tanımı (Carter):**

```csharp
public class CreateProductEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("/products", async (CreateProductRequest request, ISender sender) =>
        {
            var command = request.Adapt<CreateProductCommand>();
            var result = await sender.Send(command);
            var response = result.Adapt<CreateProductResponse>();
            return Results.Created($"/products/{response.Id}", response);
        })
        .WithName("CreateProduct")
        .Produces<CreateProductResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Create Product")
        .WithDescription("Create Product");
    }
}

public record CreateProductRequest(string Name, List<string> Category, string Description, string ImageFile, decimal Price);
public record CreateProductResponse(Guid Id);
```

#### 2. Basket API - Redis Cache İmplementasyonu

**Repository Pattern:**

```csharp
public class BasketRepository : IBasketRepository
{
    private readonly IDatabase _database;

    public BasketRepository(IConnectionMultiplexer redis)
    {
        _database = redis.GetDatabase();
    }

    public async Task<ShoppingCart> GetBasket(string userName)
    {
        var basket = await _database.StringGetAsync(userName);

        if (string.IsNullOrEmpty(basket))
            return null;

        return JsonSerializer.Deserialize<ShoppingCart>(basket, JsonOptions);
    }

    public async Task<ShoppingCart> StoreBasket(ShoppingCart basket)
    {
        await _database.StringSetAsync(basket.UserName,
            JsonSerializer.Serialize(basket, JsonOptions));

        return await GetBasket(basket.UserName);
    }

    public async Task<bool> DeleteBasket(string userName)
    {
        return await _database.KeyDeleteAsync(userName);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
```

**Cached Repository Decorator:**

```csharp
public class CachedBasketRepository : IBasketRepository
{
    private readonly IBasketRepository _repository;
    private readonly IMemoryCache _cache;

    public CachedBasketRepository(IBasketRepository repository, IMemoryCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<ShoppingCart> GetBasket(string userName)
    {
        var cacheKey = $"basket_{userName}";

        if (_cache.TryGetValue(cacheKey, out ShoppingCart cachedBasket))
            return cachedBasket;

        var basket = await _repository.GetBasket(userName);

        if (basket != null)
        {
            _cache.Set(cacheKey, basket, TimeSpan.FromMinutes(30));
        }

        return basket;
    }

    public async Task<ShoppingCart> StoreBasket(ShoppingCart basket)
    {
        var result = await _repository.StoreBasket(basket);

        // Cache'i güncelle
        var cacheKey = $"basket_{basket.UserName}";
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

        return result;
    }

    public async Task<bool> DeleteBasket(string userName)
    {
        var result = await _repository.DeleteBasket(userName);

        // Cache'den sil
        var cacheKey = $"basket_{userName}";
        _cache.Remove(cacheKey);

        return result;
    }
}
```

#### 3. gRPC Service İmplementasyonu

**Discount Service:**

```csharp
public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    private readonly DiscountContext _dbContext;
    private readonly ILogger<DiscountService> _logger;

    public DiscountService(DiscountContext dbContext, ILogger<DiscountService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var coupon = await _dbContext
            .Coupons
            .FirstOrDefaultAsync(x => x.ProductName == request.ProductName);

        if (coupon is null)
        {
            coupon = new Coupon
            {
                ProductName = "No Discount",
                Amount = 0,
                Description = "No Discount Desc"
            };
        }

        _logger.LogInformation("Discount is retrieved for ProductName: {ProductName}, Amount: {Amount}",
            coupon.ProductName, coupon.Amount);

        var couponModel = new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Description = coupon.Description,
            Amount = coupon.Amount
        };

        return couponModel;
    }
}
```

#### 4. Event Handler İmplementasyonu

**Domain Event Handler:**

```csharp
public class OrderCreatedEventHandler : INotificationHandler<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedEventHandler> _logger;

    public OrderCreatedEventHandler(ILogger<OrderCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCreatedEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Domain Event handled: {DomainEvent}", domainEvent.GetType().Name);

        // İş kuralları burada işlenebilir:
        // - Email gönderimi
        // - Stok güncelleme
        // - Bildirim gönderme
        // - Audit log yazma

        return Task.CompletedTask;
    }
}
```

#### 5. Health Checks İmplementasyonu

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Database")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!)
    .AddRabbitMQ(rabbitConnectionString: builder.Configuration.GetConnectionString("MessageBroker")!);

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

### 🎯 Best Practices ve İpuçları

1. **Dependency Injection**: Her servis için DI container kullanın
2. **Configuration**: appsettings.json ve environment variables
3. **Logging**: Structured logging ile Serilog kullanın
4. **Error Handling**: Global exception handler implementasyonu
5. **Validation**: FluentValidation ile business rules
6. **Testing**: Unit, Integration ve End-to-End testler
7. **Monitoring**: Health checks ve metrics
8. **Security**: JWT token authentication
9. **Documentation**: OpenAPI/Swagger documentation
10. **Performance**: Caching strategies ve optimization

### 🚀 Öğrenme Yolu

1. **Başlangıç**: Tek bir mikroservis ile başlayın (Catalog)
2. **İkinci Adım**: Veritabanı entegrasyonu (PostgreSQL + Marten)
3. **Üçüncü Adım**: API Gateway entegrasyonu (YARP)
4. **Dördüncü Adım**: İkinci mikroservis (Basket + Redis)
5. **Beşinci Adım**: gRPC iletişim (Discount service)
6. **Altıncı Adım**: Event-driven communication (RabbitMQ)
7. **Yedinci Adım**: Complex domain logic (Ordering with DDD)
8. **Sekizinci Adım**: Frontend integration (Shopping.Web)
9. **Dokuzuncu Adım**: Containerization (Docker)
10. **Onuncu Adım**: Production considerations (Monitoring, Security)

Bu rehber ile .NET 8 mikroservis mimarisini adım adım öğrenebilir ve gerçek projelerinizde uygulayabilirsiniz!
