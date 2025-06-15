# .NET 8 Mikroservis Öğrenme Rotası ve Pratik Projeler

## 🎯 Öğrenme Hedefleri

Bu rehber ile şunları öğreneceksiniz:

- Modern .NET 8 mikroservis mimarisi
- Domain-Driven Design (DDD) principles
- CQRS pattern implementation
- Event-driven architecture
- API Gateway patterns (YARP)
- Container orchestration (Docker)

---

## 📚 Seviye Seviye Öğrenme Rotası

### 🥉 **Seviye 1: Temel Kavramlar (1-2 Hafta)**

#### Öğrenilecekler:

- .NET 8 yenilikleri
- Minimal APIs
- Dependency Injection
- Entity Framework Core basics
- Docker fundamentals

#### Pratik Proje: **Basit Product API**

```csharp
// Hedef: Tek mikroservis ile başlayın
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

// Minimal API endpoints
app.MapGet("/products", (ApplicationDbContext db) =>
    db.Products.ToListAsync());

app.MapPost("/products", (Product product, ApplicationDbContext db) =>
{
    db.Products.Add(product);
    db.SaveChanges();
    return Results.Created($"/products/{product.Id}", product);
});
```

#### Ödevler:

1. ✅ Basit CRUD API oluşturun
2. ✅ Docker container'ı hazırlayın
3. ✅ Swagger documentation ekleyin
4. ✅ Health checks implement edin

---

### 🥈 **Seviye 2: Mikroservis Temelleri (2-3 Hafta)**

#### Öğrenilecekler:

- CQRS pattern
- MediatR library
- Repository pattern
- PostgreSQL + Marten
- Redis caching

#### Pratik Proje: **Catalog Mikroservisi Clone**

```csharp
// 1. CQRS Implementation
public record GetProductsQuery : IQuery<GetProductsResult>;
public record GetProductsResult(IEnumerable<Product> Products);

public class GetProductsHandler : IQueryHandler<GetProductsQuery, GetProductsResult>
{
    private readonly IDocumentSession _session;

    public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var products = await _session.Query<Product>().ToListAsync(cancellationToken);
        return new GetProductsResult(products);
    }
}

// 2. Carter Endpoints
public class ProductEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/products", async (ISender sender) =>
        {
            var result = await sender.Send(new GetProductsQuery());
            return Results.Ok(result.Products);
        });
    }
}
```

#### Ödevler:

1. ✅ Marten ile document database kullanın
2. ✅ CQRS pattern implement edin
3. ✅ Carter ile minimal API'ler oluşturun
4. ✅ Pagination ekleyin
5. ✅ Exception handling implement edin

---

### 🥇 **Seviye 3: İleri Mikroservis Teknikleri (3-4 Hafta)**

#### Öğrenilecekler:

- Domain-Driven Design (DDD)
- Aggregate patterns
- Value objects
- Domain events
- Clean architecture

#### Pratik Proje: **Ordering Mikroservisi Clone**

```csharp
// 1. Value Object
public record Address
{
    public string Street { get; init; }
    public string City { get; init; }
    public string Country { get; init; }

    public static Address Of(string street, string city, string country)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        return new Address { Street = street, City = city, Country = country };
    }
}

// 2. Aggregate Root
public class Order : Aggregate<OrderId>
{
    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();
    public CustomerId CustomerId { get; private set; }
    public Address ShippingAddress { get; private set; }

    public static Order Create(OrderId id, CustomerId customerId, Address shippingAddress)
    {
        var order = new Order
        {
            Id = id,
            CustomerId = customerId,
            ShippingAddress = shippingAddress
        };

        order.AddDomainEvent(new OrderCreatedEvent(order));
        return order;
    }

    public void AddItem(ProductId productId, int quantity, decimal price)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        var orderItem = new OrderItem(Id, productId, quantity, price);
        _orderItems.Add(orderItem);
    }
}

// 3. Domain Event Handler
public class OrderCreatedEventHandler : INotificationHandler<OrderCreatedEvent>
{
    public Task Handle(OrderCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Business logic: Send email, update inventory, etc.
        return Task.CompletedTask;
    }
}
```

#### Ödevler:

1. ✅ Clean Architecture katmanları oluşturun
2. ✅ Value objects implement edin
3. ✅ Aggregate patterns kullanın
4. ✅ Domain events ekleyin
5. ✅ EF Core complex configurations yapın

---

### 🏆 **Seviye 4: Servisler Arası İletişim (3-4 Hafta)**

#### Öğrenilecekler:

- gRPC inter-service communication
- RabbitMQ messaging
- MassTransit framework
- Event-driven architecture
- Saga patterns

#### Pratik Proje: **Basket + Discount Integration**

```csharp
// 1. gRPC Service Definition (.proto)
syntax = "proto3";
service DiscountService {
    rpc GetDiscount (GetDiscountRequest) returns (CouponModel);
}

message GetDiscountRequest {
    string productName = 1;
}

message CouponModel {
    int32 id = 1;
    string productName = 2;
    int32 amount = 3;
}

// 2. gRPC Service Implementation
public class DiscountService : DiscountProtoService.DiscountProtoServiceBase
{
    public override async Task<CouponModel> GetDiscount(GetDiscountRequest request, ServerCallContext context)
    {
        var coupon = await _repository.GetDiscountAsync(request.ProductName);
        return new CouponModel
        {
            Id = coupon.Id,
            ProductName = coupon.ProductName,
            Amount = coupon.Amount
        };
    }
}

// 3. Event Publishing
public class BasketCheckoutHandler : ICommandHandler<CheckoutBasketCommand>
{
    public async Task Handle(CheckoutBasketCommand command, CancellationToken cancellationToken)
    {
        // Get basket
        var basket = await _repository.GetBasketAsync(command.UserName);

        // Publish event
        var checkoutEvent = new BasketCheckoutEvent
        {
            UserName = command.UserName,
            TotalPrice = basket.TotalPrice,
            Items = basket.Items
        };

        await _publisher.Publish(checkoutEvent, cancellationToken);

        // Clear basket
        await _repository.DeleteBasketAsync(command.UserName);
    }
}

// 4. Event Consuming
public class BasketCheckoutEventHandler : IConsumer<BasketCheckoutEvent>
{
    public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
    {
        var createOrderCommand = new CreateOrderCommand(context.Message);
        await _mediator.Send(createOrderCommand);
    }
}
```

#### Ödevler:

1. ✅ gRPC service oluşturun ve consume edin
2. ✅ RabbitMQ ile event publishing implement edin
3. ✅ MassTransit configuration yapın
4. ✅ Event handler'lar yazın
5. ✅ Resilience patterns ekleyin (retry, circuit breaker)

---

### 💎 **Seviye 5: API Gateway ve Frontend (2-3 Hafta)**

#### Öğrenilecekler:

- YARP reverse proxy
- Rate limiting
- Request/Response transformation
- ASP.NET Core Razor Pages
- Refit HTTP client

#### Pratik Proje: **Complete E-commerce Flow**

```csharp
// 1. YARP Configuration
{
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "RateLimiterPolicy": "fixed-window",
        "Match": { "Path": "/catalog/{**catch-all}" },
        "Transforms": [
          { "PathPattern": "/products/{**catch-all}" },
          { "RequestHeader": "X-Forwarded-For", "Set": "{RemoteIpAddress}" }
        ]
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "LoadBalancingPolicy": "RoundRobin",
        "Destinations": {
          "catalog1": { "Address": "http://catalog.api:8080" },
          "catalog2": { "Address": "http://catalog.api2:8080" }
        }
      }
    }
  }
}

// 2. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed-window", limiterOptions =>
    {
        limiterOptions.Window = TimeSpan.FromSeconds(10);
        limiterOptions.PermitLimit = 10;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });
});

// 3. Frontend Service (Refit)
public interface ICatalogService
{
    [Get("/catalog/products")]
    Task<ApiResponse<IEnumerable<ProductModel>>> GetProductsAsync();

    [Get("/catalog/products/{id}")]
    Task<ApiResponse<ProductModel>> GetProductAsync(Guid id);

    [Post("/catalog/products")]
    Task<ApiResponse<ProductModel>> CreateProductAsync([Body] CreateProductRequest request);
}

// 4. Razor Page with Error Handling
public class ProductListModel : PageModel
{
    private readonly ICatalogService _catalogService;

    public IEnumerable<ProductModel> Products { get; set; } = new List<ProductModel>();
    public string ErrorMessage { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            var response = await _catalogService.GetProductsAsync();
            if (response.IsSuccessStatusCode)
            {
                Products = response.Content ?? new List<ProductModel>();
            }
            else
            {
                ErrorMessage = $"API Error: {response.Error?.Content}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "Service unavailable. Please try again later.";
            _logger.LogError(ex, "Error fetching products");
        }

        return Page();
    }
}
```

#### Ödevler:

1. ✅ YARP gateway configuration yapın
2. ✅ Rate limiting implement edin
3. ✅ Load balancing test edin
4. ✅ Frontend uygulaması geliştirin
5. ✅ Error handling ve resilience ekleyin

---

### 🚀 **Seviye 6: Production Ready (3-4 Hafta)**

#### Öğrenilecekler:

- Docker Compose orchestration
- Health checks & monitoring
- Logging & observability
- Security implementations
- Performance optimization

#### Pratik Proje: **Full Production Setup**

```yaml
# docker-compose.yml - Production Ready
version: "3.8"

services:
  # Databases
  postgres-catalog:
    image: postgres:15
    environment:
      POSTGRES_DB: CatalogDb
      POSTGRES_USER: ${DB_USER:-postgres}
      POSTGRES_PASSWORD: ${DB_PASSWORD:-postgres}
    volumes:
      - postgres_catalog_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${DB_USER:-postgres}"]
      interval: 30s
      timeout: 10s
      retries: 3
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M

  # Redis with persistence
  redis:
    image: redis:7-alpine
    command: redis-server --appendonly yes
    volumes:
      - redis_data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

  # RabbitMQ with clustering
  rabbitmq:
    image: rabbitmq:3-management-alpine
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:-guest}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASS:-guest}
      RABBITMQ_ERLANG_COOKIE: ${RABBITMQ_COOKIE:-unique_cookie}
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 30s
      retries: 3

  # Microservices with health checks
  catalog-api:
    build:
      context: .
      dockerfile: src/Services/Catalog/Catalog.API/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=${ENVIRONMENT:-Production}
      - ConnectionStrings__Database=Server=postgres-catalog;Database=CatalogDb;User Id=${DB_USER:-postgres};Password=${DB_PASSWORD:-postgres}
      - Serilog__WriteTo__0__Name=Console
      - Serilog__WriteTo__1__Name=File
      - Serilog__WriteTo__1__Args__path=/app/logs/catalog-.log
      - Serilog__WriteTo__1__Args__rollingInterval=Day
    depends_on:
      postgres-catalog:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    deploy:
      replicas: 2
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M
    volumes:
      - catalog_logs:/app/logs

  # API Gateway with SSL
  api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateways/YarpApiGateway/Dockerfile
    environment:
      - ASPNETCORE_ENVIRONMENT=${ENVIRONMENT:-Production}
      - ASPNETCORE_URLS=https://+:443;http://+:80
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/aspnetapp.pfx
    volumes:
      - ./certs:/https:ro
      - gateway_logs:/app/logs
    ports:
      - "80:80"
      - "443:443"
    depends_on:
      - catalog-api
      - basket-api
      - ordering-api
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:80/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  # Monitoring & Observability
  prometheus:
    image: prom/prometheus:latest
    volumes:
      - ./monitoring/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus_data:/prometheus
    ports:
      - "9090:9090"

  grafana:
    image: grafana/grafana:latest
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=${GRAFANA_PASSWORD:-admin}
    volumes:
      - grafana_data:/var/lib/grafana
      - ./monitoring/grafana/dashboards:/etc/grafana/provisioning/dashboards
    ports:
      - "3000:3000"
    depends_on:
      - prometheus

volumes:
  postgres_catalog_data:
  redis_data:
  rabbitmq_data:
  catalog_logs:
  gateway_logs:
  prometheus_data:
  grafana_data:

networks:
  default:
    driver: bridge
    ipam:
      config:
        - subnet: 172.20.0.0/16
```

```csharp
// Health Checks Implementation
public static class HealthCheckExtensions
{
    public static IServiceCollection AddCustomHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            // Database health checks
            .AddNpgSql(configuration.GetConnectionString("Database")!,
                      name: "catalog-db",
                      tags: new[] { "database", "postgresql" })

            // Cache health checks
            .AddRedis(configuration.GetConnectionString("Redis")!,
                     name: "redis-cache",
                     tags: new[] { "cache", "redis" })

            // Message broker health checks
            .AddRabbitMQ(configuration.GetConnectionString("MessageBroker")!,
                        name: "rabbitmq",
                        tags: new[] { "messagebroker", "rabbitmq" })

            // External service health checks
            .AddUrlGroup(new Uri($"{configuration["ApiSettings:GatewayAddress"]}/health"),
                        "api-gateway",
                        tags: new[] { "gateway", "external" })

            // Custom business logic health checks
            .AddCheck<DatabaseSeededHealthCheck>("database-seeded")
            .AddCheck<DiskSpaceHealthCheck>("disk-space");

        return services;
    }
}

// Custom Health Check
public class DatabaseSeededHealthCheck : IHealthCheck
{
    private readonly IDocumentSession _session;

    public DatabaseSeededHealthCheck(IDocumentSession session)
    {
        _session = session;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var productCount = await _session.Query<Product>().CountAsync(cancellationToken);

            if (productCount == 0)
            {
                return HealthCheckResult.Degraded("Database is not seeded with initial data");
            }

            return HealthCheckResult.Healthy($"Database contains {productCount} products");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}

// Structured Logging with Serilog
public static class LoggingExtensions
{
    public static IServiceCollection AddCustomSerilog(this IServiceCollection services, IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithProperty("Application", "CatalogAPI")
            .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File("logs/catalog-.log",
                         rollingInterval: RollingInterval.Day,
                         retainedFileCountLimit: 7,
                         outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.Seq(configuration.GetConnectionString("Seq") ?? "http://localhost:5341")
            .CreateLogger();

        services.AddSerilog();
        return services;
    }
}

// Performance Monitoring
public class PerformanceMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceMiddleware> _logger;

    public PerformanceMiddleware(RequestDelegate next, ILogger<PerformanceMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();

            if (sw.ElapsedMilliseconds > 100) // Log slow requests
            {
                _logger.LogWarning("Slow request: {Method} {Path} took {ElapsedMilliseconds}ms",
                    context.Request.Method,
                    context.Request.Path,
                    sw.ElapsedMilliseconds);
            }

            // Add response time header
            context.Response.Headers.Append("X-Response-Time", $"{sw.ElapsedMilliseconds}ms");
        }
    }
}
```

#### Final Ödevler:

1. ✅ Production-ready Docker Compose setup
2. ✅ Comprehensive health checks
3. ✅ Structured logging with Serilog
4. ✅ Monitoring with Prometheus/Grafana
5. ✅ SSL/TLS configuration
6. ✅ Performance optimization
7. ✅ Security hardening
8. ✅ Load testing and optimization

---

## 🎯 Pratik Proje Önerileri

### **Mini Proje 1: Personal Blog Mikroservisleri**

- **Posts Service**: Blog yazıları
- **Comments Service**: Yorumlar
- **Users Service**: Kullanıcı yönetimi
- **Notifications Service**: Bildirimler

### **Mini Proje 2: Task Management System**

- **Projects Service**: Proje yönetimi
- **Tasks Service**: Görev yönetimi
- **Users Service**: Kullanıcılar
- **Notifications Service**: Bildirimler

### **Mini Proje 3: Simple Banking System**

- **Accounts Service**: Hesap yönetimi
- **Transactions Service**: Para transferi
- **Notifications Service**: SMS/Email
- **Audit Service**: Denetim kayıtları

---

## 📖 Önerilen Kaynaklar

### **Kitaplar:**

1. **"Microservices Patterns"** - Chris Richardson
2. **"Building Microservices"** - Sam Newman
3. **"Domain-Driven Design"** - Eric Evans
4. **"Clean Architecture"** - Robert C. Martin

### **Online Kaynaklar:**

1. **Microsoft .NET Documentation**
2. **Mehmet Özkaya's Udemy Course** (Bu projenin yaratıcısı)
3. **Martin Fowler's Blog** (Microservices articles)
4. **ASP.NET Core YouTube Channel**

### **GitHub Repositories:**

1. **aspnetrun/run-aspnetcore-microservices** (Bu proje)
2. **dotnet/eShop** (Microsoft's reference application)
3. **madslundt/NetCoreMicroservicesSample**

---

## ✅ Başarı Kriterleri

### **Seviye 1 Tamamlandı:**

- [ ] Basit CRUD API yazabiliyorum
- [ ] Docker container oluşturabiliyorum
- [ ] Swagger documentation kullanabiliyorum
- [ ] Health checks implement edebiliyorum

### **Seviye 2 Tamamlandı:**

- [ ] CQRS pattern kullanabiliyorum
- [ ] MediatR ile command/query handling yapabiliyorum
- [ ] Marten ile document database kullanabiliyorum
- [ ] Carter ile minimal API'ler yazabiliyorum

### **Seviye 3 Tamamlandı:**

- [ ] Clean Architecture uygulayabiliyorum
- [ ] Value objects oluşturabiliyorum
- [ ] Aggregate patterns kullanabiliyorum
- [ ] Domain events implement edebiliyorum

### **Seviye 4 Tamamlandı:**

- [ ] gRPC service yazabiliyorum
- [ ] RabbitMQ ile event-driven architecture kurabıliyorum
- [ ] MassTransit configuration yapabiliyorum
- [ ] Inter-service communication implement edebiliyorum

### **Seviye 5 Tamamlandı:**

- [ ] YARP API Gateway kurabıliyorum
- [ ] Rate limiting uygulayabiliyorum
- [ ] Frontend service integration yapabiliyorum
- [ ] Refit HTTP client kullanabiliyorum

### **Seviye 6 Tamamlandı:**

- [ ] Production-ready setup yapabiliyorum
- [ ] Monitoring ve logging implement edebiliyorum
- [ ] Security best practices uygulayabiliyorum
- [ ] Performance optimization yapabiliyorum

---

## 🏃‍♂️ Hızlı Başlangıç

```bash
# 1. Repository'yi clone edin
git clone https://github.com/aspnetrun/run-aspnetcore-microservices.git
cd run-aspnetcore-microservices

# 2. Development environment'ı başlatın
cd src
docker-compose -f docker-compose.yml -f docker-compose.override.yml up -d

# 3. Servislerin ayakta olduğunu kontrol edin
docker-compose ps

# 4. Shopping Web UI'a erişin
# http://localhost:6005

# 5. API Gateway'i test edin
# http://localhost:6004/swagger
```

Bu rotayı takip ederek, modern .NET 8 mikroservis mimarisini profesyonel seviyede öğrenebilir ve gerçek projelerinizde uygulayabilirsiniz!

---

## 💡 İpuçları

1. **Adım Adım İlerleyin**: Her seviyeyi tam öğrenmeden sonrakine geçmeyin
2. **Pratik Yapın**: Teorik bilgiyi mutlaka kod yazarak pekiştirin
3. **Debug Edin**: Hata aldığınızda panic yapmayın, debug teknikleri öğrenin
4. **Community**: .NET community'sine katılın, sorular sorun
5. **Güncel Kalın**: .NET'in hızla gelişen teknolojileri takip edin

Bu rehberle birlikte, .NET 8 mikroservis dünyasında uzman olma yolculuğunuza başlayabilirsiniz! 🚀
