# .NET 8 Mikroservis Projesi - Pratik Kod Örnekleri ve Analiz

## 📋 Gerçek Kod Örnekleri ile Öğrenme

### 1. 📦 Catalog API - Vertical Slice Architecture

#### Product Model (Domain Entity)

```csharp
// src/Services/Catalog/Catalog.API/Models/Product.cs
namespace Catalog.API.Models;

public class Product
{
    public Guid Id { get; set; }                    // Primary Key
    public string Name { get; set; } = default!;    // Ürün adı
    public List<string> Category { get; set; } = new(); // Kategoriler (JSON array olarak saklanır)
    public string Description { get; set; } = default!; // Açıklama
    public string ImageFile { get; set; } = default!;   // Resim dosyası yolu
    public decimal Price { get; set; }              // Fiyat
}
```

**Önemli Noktalar:**

- `List<string> Category`: PostgreSQL'de JSON array olarak saklanır
- `default!`: Null-forgiving operator (EF Core tarafından set edilecek)
- Document-based yaklaşım (Marten ile)

#### CQRS Query Implementation

```csharp
// src/Services/Catalog/Catalog.API/Products/GetProducts/GetProductsHandler.cs

// Query Definition (CQRS)
public record GetProductsQuery : IQuery<GetProductsResult>;
public record GetProductsPaginatedQuery(int? PageNumber = 1, int? PageSize = 12) : IQuery<GetProductsResult>;
public record GetProductsResult(IEnumerable<Product> Products);

// Query Handler - Basit Tüm Ürünler
internal class GetProductsQueryHandler
    (IDocumentSession session)  // Primary constructor syntax (C# 12)
    : IQueryHandler<GetProductsQuery, GetProductsResult>
{
    public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        // Marten ile PostgreSQL'den document olarak okuma
        var products = await session.Query<Product>()
            .ToListAsync(cancellationToken);

        return new GetProductsResult(products);
    }
}

// Query Handler - Sayfalanmış Ürünler
internal class GetProductsPaginatedQueryHandler
    (IDocumentSession session)
    : IQueryHandler<GetProductsPaginatedQuery, GetProductsResult>
{
    public async Task<GetProductsResult> Handle(GetProductsPaginatedQuery query, CancellationToken cancellationToken)
    {
        // Pagination ile ürün getirme
        var products = await session.Query<Product>()
            .ToPagedListAsync(query.PageNumber ?? 1, query.PageSize ?? 12, cancellationToken);

        return new GetProductsResult(products);
    }
}
```

**Analiz:**

- **Primary Constructor**: C# 12 özelliği, dependency injection için
- **Record Types**: Immutable data containers
- **Marten IDocumentSession**: PostgreSQL document database erişimi
- **ToPagedListAsync**: Custom extension method (BuildingBlocks'ta tanımlı)

#### Carter Minimal API Endpoints

```csharp
// src/Services/Catalog/Catalog.API/Products/GetProducts/GetProductsEndpoint.cs

public record GetProductsResponse(IEnumerable<Product> Products);

public class GetProductsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        // Basit endpoint - Tüm ürünler
        app.MapGet("/products", async (ISender sender) =>
        {
            var query = new GetProductsQuery();
            var result = await sender.Send(query);          // MediatR send
            var response = result.Adapt<GetProductsResponse>(); // Mapster mapping
            return Results.Ok(response);
        })
        .WithName("GetAllProducts")
        .Produces<GetProductsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get All Products")
        .WithDescription("Get all products without pagination");

        // Sayfalanmış endpoint
        app.MapGet("/products/paginated", async ([AsParameters] GetProductsPaginatedQuery query, ISender sender) =>
        {
            var result = await sender.Send(query);
            var response = result.Adapt<GetProductsResponse>();
            return Results.Ok(response);
        })
        .WithName("GetPaginatedProducts")
        .Produces<GetProductsResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .WithSummary("Get Paginated Products")
        .WithDescription("Get products with pagination");
    }
}
```

**Analiz:**

- **Carter**: Minimal API'ler için modüler yaklaşım
- **ISender**: MediatR interface (CQRS için)
- **Mapster**: Object mapping (.Adapt<> extension)
- **[AsParameters]**: Query parametrelerini otomatik bind etme

---

### 2. 🛒 Basket API - Cache Pattern Implementation

#### Repository Pattern with PostgreSQL

```csharp
// src/Services/Basket/Basket.API/Data/BasketRepository.cs

public class BasketRepository(IDocumentSession session)
    : IBasketRepository
{
    public async Task<ShoppingCart> GetBasket(string userName, CancellationToken cancellationToken = default)
    {
        // Marten ile document yükleme
        var basket = await session.LoadAsync<ShoppingCart>(userName, cancellationToken);

        // Custom exception fırlatma
        return basket is null ? throw new BasketNotFoundException(userName) : basket;
    }

    public async Task<ShoppingCart> StoreBasket(ShoppingCart basket, CancellationToken cancellationToken = default)
    {
        // Document store pattern
        session.Store(basket);
        await session.SaveChangesAsync(cancellationToken);
        return basket;
    }

    public async Task<bool> DeleteBasket(string userName, CancellationToken cancellationToken = default)
    {
        // Document silme
        session.Delete<ShoppingCart>(userName);
        await session.SaveChangesAsync(cancellationToken);
        return true;
    }
}
```

#### Decorator Pattern - Distributed Cache

```csharp
// src/Services/Basket/Basket.API/Data/CachedBasketRepository.cs

public class CachedBasketRepository
    (IBasketRepository repository, IDistributedCache cache)
    : IBasketRepository
{
    public async Task<ShoppingCart> GetBasket(string userName, CancellationToken cancellationToken = default)
    {
        // 1. Önce cache'den kontrol et
        var cachedBasket = await cache.GetStringAsync(userName, cancellationToken);
        if (!string.IsNullOrEmpty(cachedBasket))
            return JsonSerializer.Deserialize<ShoppingCart>(cachedBasket)!;

        // 2. Cache'de yoksa database'den getir
        var basket = await repository.GetBasket(userName, cancellationToken);

        // 3. Cache'e kaydet
        await cache.SetStringAsync(userName, JsonSerializer.Serialize(basket), cancellationToken);
        return basket;
    }

    public async Task<ShoppingCart> StoreBasket(ShoppingCart basket, CancellationToken cancellationToken = default)
    {
        // 1. Database'i güncelle
        await repository.StoreBasket(basket, cancellationToken);

        // 2. Cache'i güncelle
        await cache.SetStringAsync(basket.UserName, JsonSerializer.Serialize(basket), cancellationToken);

        return basket;
    }

    public async Task<bool> DeleteBasket(string userName, CancellationToken cancellationToken = default)
    {
        // 1. Database'den sil
        await repository.DeleteBasket(userName, cancellationToken);

        // 2. Cache'den sil
        await cache.RemoveAsync(userName, cancellationToken);

        return true;
    }
}
```

**Analiz:**

- **Decorator Pattern**: Repository'yi cache ile sarlıyor
- **Cache-Aside Pattern**: Önce cache, sonra database
- **IDistributedCache**: Redis implementation (DI ile gelir)
- **JSON Serialization**: Cache'e JSON string olarak saklama

---

### 3. 📋 Ordering API - Domain-Driven Design

#### Aggregate Root Pattern

```csharp
// src/Services/Ordering/Ordering.Domain/Models/Order.cs

public class Order : Aggregate<OrderId>  // Aggregate Root
{
    private readonly List<OrderItem> _orderItems = new();
    public IReadOnlyList<OrderItem> OrderItems => _orderItems.AsReadOnly();

    // Value Objects
    public CustomerId CustomerId { get; private set; } = default!;
    public OrderName OrderName { get; private set; } = default!;
    public Address ShippingAddress { get; private set; } = default!;
    public Address BillingAddress { get; private set; } = default!;
    public Payment Payment { get; private set; } = default!;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;

    // Calculated Property
    public decimal TotalPrice
    {
        get => OrderItems.Sum(x => x.Price * x.Quantity);
        private set { } // EF Core için gerekli
    }

    // Factory Method (Domain Logic)
    public static Order Create(OrderId id, CustomerId customerId, OrderName orderName,
                              Address shippingAddress, Address billingAddress, Payment payment)
    {
        var order = new Order
        {
            Id = id,
            CustomerId = customerId,
            OrderName = orderName,
            ShippingAddress = shippingAddress,
            BillingAddress = billingAddress,
            Payment = payment,
            Status = OrderStatus.Pending
        };

        // Domain Event fırlatma
        order.AddDomainEvent(new OrderCreatedEvent(order));
        return order;
    }

    // Business Logic Method
    public void Update(OrderName orderName, Address shippingAddress, Address billingAddress,
                      Payment payment, OrderStatus status)
    {
        OrderName = orderName;
        ShippingAddress = shippingAddress;
        BillingAddress = billingAddress;
        Payment = payment;
        Status = status;

        // Domain Event
        AddDomainEvent(new OrderUpdatedEvent(this));
    }

    // Business Rule: Add Item
    public void Add(ProductId productId, int quantity, decimal price)
    {
        // Validation
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        var orderItem = new OrderItem(Id, productId, quantity, price);
        _orderItems.Add(orderItem);
    }

    // Business Rule: Remove Item
    public void Remove(ProductId productId)
    {
        var orderItem = _orderItems.FirstOrDefault(x => x.ProductId == productId);
        if (orderItem is not null)
        {
            _orderItems.Remove(orderItem);
        }
    }
}
```

#### Value Objects Implementation

```csharp
// src/Services/Ordering/Ordering.Domain/ValueObjects/Address.cs

public record Address
{
    public string FirstName { get; } = default!;
    public string LastName { get; } = default!;
    public string? EmailAddress { get; } = default!;
    public string AddressLine { get; } = default!;
    public string Country { get; } = default!;
    public string State { get; } = default!;
    public string ZipCode { get; } = default!;

    // Private constructor - Domain kontrolü için
    protected Address() { }

    private Address(string firstName, string lastName, string emailAddress,
                   string addressLine, string country, string state, string zipCode)
    {
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
        AddressLine = addressLine;
        Country = country;
        State = state;
        ZipCode = zipCode;
    }

    // Factory Method with Validation
    public static Address Of(string firstName, string lastName, string emailAddress,
                           string addressLine, string country, string state, string zipCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(addressLine);

        return new Address(firstName, lastName, emailAddress, addressLine, country, state, zipCode);
    }
}

// src/Services/Ordering/Ordering.Domain/ValueObjects/OrderId.cs
public record OrderId
{
    public Guid Value { get; }

    private OrderId(Guid value) => Value = value;

    public static OrderId Of(Guid value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (value == Guid.Empty)
        {
            throw new DomainException("OrderId cannot be empty.");
        }
        return new OrderId(value);
    }
}
```

**DDD Analizi:**

- **Aggregate Root**: Order entity, tüm değişiklikleri kontrol eder
- **Value Objects**: Immutable, business logic içerir
- **Factory Methods**: Domain validation ile object creation
- **Private Setters**: Encapsulation, sadece domain methods ile değişim
- **Domain Events**: Business olayları fırlatma

---

### 4. 🚪 YARP API Gateway Configuration

#### appsettings.json Configuration

```json
{
  "ReverseProxy": {
    "Routes": {
      "catalog-route": {
        "ClusterId": "catalog-cluster",
        "Match": {
          "Path": "/catalog-service/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "{**catch-all}" }]
      },
      "basket-route": {
        "ClusterId": "basket-cluster",
        "Match": {
          "Path": "/basket-service/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "{**catch-all}" }]
      },
      "ordering-route": {
        "ClusterId": "ordering-cluster",
        "RateLimiterPolicy": "fixed",
        "Match": {
          "Path": "/ordering-service/{**catch-all}"
        },
        "Transforms": [{ "PathPattern": "{**catch-all}" }]
      }
    },
    "Clusters": {
      "catalog-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://catalog.api:8080"
          }
        }
      },
      "basket-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://basket.api:8080"
          }
        }
      },
      "ordering-cluster": {
        "Destinations": {
          "destination1": {
            "Address": "http://ordering.api:8080"
          }
        }
      }
    }
  }
}
```

**YARP Analizi:**

- **Routes**: URL routing kuralları
- **Clusters**: Backend servis grupları
- **Transforms**: URL dönüştürme kuralları
- **RateLimiterPolicy**: İstek sınırlama
- **{**catch-all}\*\*: Wildcard path matching

---

### 5. 🖥️ Shopping.Web - Frontend Implementation

#### Refit HTTP Client Usage

```csharp
// Program.cs - Service Registration
builder.Services.AddRefitClient<ICatalogService>()
    .ConfigureHttpClient(c =>
    {
        c.BaseAddress = new Uri(builder.Configuration["ApiSettings:GatewayAddress"]!);
    });

// Interface Definition
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

// Razor Page Usage
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
        try
        {
            ProductList = await _catalogService.GetProducts();
            return Page();
        }
        catch (Exception ex)
        {
            // Error handling
            TempData["Error"] = "Ürünler yüklenirken hata oluştu.";
            return Page();
        }
    }
}
```

---

### 6. 🐳 Docker ve Konteynerizasyon

#### docker-compose.yml Analizi

```yaml
version: "3.4"

services:
  # Database Services
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

  # Cache Service
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
      - "5672:5672" # AMQP port
      - "15672:15672" # Management UI

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

#### Multi-stage Dockerfile

```dockerfile
# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
EXPOSE 8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["Services/Catalog/Catalog.API/Catalog.API.csproj", "Services/Catalog/Catalog.API/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]

# Restore dependencies
RUN dotnet restore "./Services/Catalog/Catalog.API/Catalog.API.csproj"

# Copy source code
COPY . .
WORKDIR "/src/Services/Catalog/Catalog.API"

# Build application
RUN dotnet build "./Catalog.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "./Catalog.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Catalog.API.dll"]
```

---

### 7. 🔄 Event-Driven Architecture Implementation

#### Event Publishing (Basket → Ordering)

```csharp
// Basket.API - Checkout Handler
public class CheckoutBasketHandler : ICommandHandler<CheckoutBasketCommand, CheckoutBasketResult>
{
    public async Task<CheckoutBasketResult> Handle(CheckoutBasketCommand command, CancellationToken cancellationToken)
    {
        // 1. Get basket from cache/database
        var basket = await repository.GetBasket(command.BasketCheckoutDto.UserName, cancellationToken);

        // 2. Create integration event
        var eventMessage = command.BasketCheckoutDto.Adapt<BasketCheckoutEvent>();
        eventMessage.TotalPrice = basket.TotalPrice;

        // 3. Publish event to RabbitMQ
        await publishEndpoint.Publish(eventMessage, cancellationToken);

        // 4. Delete basket after checkout
        await repository.DeleteBasket(command.BasketCheckoutDto.UserName, cancellationToken);

        return new CheckoutBasketResult(true);
    }
}

// Integration Event
public class BasketCheckoutEvent : IntegrationEvent
{
    public string UserName { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string EmailAddress { get; set; } = default!;
    public string AddressLine { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string State { get; set; } = default!;
    public string ZipCode { get; set; } = default!;
    // Payment information...
}
```

#### Event Consuming (Ordering)

```csharp
// Ordering.Application - Event Handler
public class BasketCheckoutEventHandler : IConsumer<BasketCheckoutEvent>
{
    private readonly ISender _sender;

    public BasketCheckoutEventHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
    {
        // Convert event to command
        var command = new CreateOrderCommand(context.Message.Adapt<OrderDto>());

        // Send command through MediatR
        var result = await _sender.Send(command);

        // Log success
        // Could also publish order created event here
    }
}

// MassTransit Configuration
builder.Services.AddMassTransit(config =>
{
    config.SetKebabCaseEndpointNameFormatter();

    // Add consumers
    config.AddConsumer<BasketCheckoutEventHandler>();

    config.UsingRabbitMq((context, configurator) =>
    {
        configurator.Host(new Uri(builder.Configuration["MessageBroker:Host"]!), host =>
        {
            host.Username(builder.Configuration["MessageBroker:UserName"]!);
            host.Password(builder.Configuration["MessageBroker:Password"]!);
        });

        configurator.ConfigureEndpoints(context);
    });
});
```

---

### 8. 🛡️ Best Practices ve Kalite Kontrolü

#### Global Exception Handling

```csharp
public class CustomExceptionHandler : IExceptionHandler
{
    private readonly ILogger<CustomExceptionHandler> _logger;

    public CustomExceptionHandler(ILogger<CustomExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError("Error Message: {exceptionMessage}, Time of occurrence {time}",
            exception.Message, DateTime.UtcNow);

        (string Detail, string Title, int StatusCode) = exception switch
        {
            InternalServerException =>
                (exception.Message, exception.GetType().Name, httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError),

            ValidationException =>
                (exception.Message, exception.GetType().Name, httpContext.Response.StatusCode = StatusCodes.Status400BadRequest),

            BadRequestException =>
                (exception.Message, exception.GetType().Name, httpContext.Response.StatusCode = StatusCodes.Status400BadRequest),

            NotFoundException =>
                (exception.Message, exception.GetType().Name, httpContext.Response.StatusCode = StatusCodes.Status404NotFound),

            _ =>
                (exception.Message, exception.GetType().Name, httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError)
        };

        var problemDetails = new ProblemDetails
        {
            Title = Title,
            Detail = Detail,
            Status = StatusCode,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions.Add("traceId", httpContext.TraceIdentifier);

        if (exception is ValidationException validationException)
        {
            problemDetails.Extensions.Add("ValidationErrors", validationException.Errors);
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken: cancellationToken);

        return true;
    }
}
```

Bu detaylı kod analizi ile .NET 8 mikroservis mimarisinin tüm yönlerini gerçek kod örnekleri ile öğrenebilirsiniz. Her pattern ve yaklaşımın pratikteki implementasyonunu görmek, öğrenme sürecini çok daha etkili kılar.
