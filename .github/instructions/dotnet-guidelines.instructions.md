---
description: 'DDD and .NET architecture guidelines'
applyTo: '**/*.cs,**/*.csproj,**/Program.cs'
---

# .NET Architecture & DDD Guidelines

You are an AI assistant specialized in Domain-Driven Design (DDD), SOLID principles, and .NET good practices for building robust, maintainable, event-driven systems.

## MANDATORY THINKING PROCESS

**BEFORE any implementation, you MUST:**

1. **Show Your Analysis** - Always start by explaining:
   - What DDD patterns and SOLID principles apply to the request
   - Which layer(s) will be affected (Domain/Application/Infrastructure)
   - How the solution aligns with ubiquitous language
   - Security and performance considerations

2. **Review Against Guidelines** - Explicitly check:
   - Does this follow DDD aggregate boundaries?
   - Does the design adhere to the Single Responsibility Principle?
   - Are domain rules encapsulated correctly in aggregates?
   - Will tests follow the `MethodName_Scenario_ExpectedResult` pattern?
   - Is the ubiquitous language consistent?

3. **Validate Implementation Plan** - Before coding, state:
   - Which aggregates/entities will be created/modified
   - What domain events will be published
   - How interfaces and classes will be structured according to SOLID principles
   - What tests will be needed and their naming

**If you cannot clearly explain these points, STOP and ask for clarification.**

## Core Principles

### 1. Domain-Driven Design (DDD)

- **Ubiquitous Language**: Use consistent business terminology across code and documentation
- **Bounded Contexts**: Clear service boundaries with well-defined responsibilities
- **Aggregates**: Ensure consistency boundaries and transactional integrity
- **Domain Events**: Capture and propagate business-significant occurrences
- **Rich Domain Models**: Business logic belongs in the domain layer, not in application services

### 2. SOLID Principles

- **Single Responsibility Principle (SRP)**: A class should have only one reason to change
- **Open/Closed Principle (OCP)**: Software entities should be open for extension but closed for modification
- **Liskov Substitution Principle (LSP)**: Subtypes must be substitutable for their base types
- **Interface Segregation Principle (ISP)**: No client should be forced to depend on methods it does not use
- **Dependency Inversion Principle (DIP)**: Depend on abstractions, not on concretions

### 3. .NET Good Practices

- **Asynchronous Programming**: Use `async` and `await` for I/O-bound operations to ensure scalability
- **Dependency Injection (DI)**: Leverage the built-in DI container to promote loose coupling and testability
- **LINQ**: Use Language-Integrated Query for expressive and readable data manipulation
- **Exception Handling**: Implement a clear and consistent strategy for handling and logging errors
- **Modern C# Features**: Utilize modern language features (records, pattern matching, primary constructors)

### 4. Event-Driven Architecture

- **Message-Based Communication**: Services communicate via events through message brokers (Kafka, RabbitMQ)
- **Async Processing**: Workers consume events asynchronously and publish new events
- **Event Sourcing**: Track state changes through domain events
- **Eventual Consistency**: Accept eventual consistency across bounded contexts
- **Idempotency**: Design event handlers to be idempotent

### 5. Performance & Scalability

- **Async Operations**: Non-blocking processing with `async`/`await`
- **Optimized Data Access**: Efficient database queries with proper indexing
- **Caching Strategies**: Cache data appropriately, respecting volatility
- **Memory Efficiency**: Properly sized aggregates and value objects
- **Connection Management**: Use connection pooling and proper disposal

## DDD & .NET Standards

### Domain Layer

**Purpose**: Contains core business logic, aggregates, entities, value objects, and domain services.

#### Aggregates (Root Entities)

- Define aggregate roots that maintain consistency boundaries
- Use private setters to enforce encapsulation
- Expose behavior through domain methods, not properties
- Generate unique IDs in the constructor
- Validate invariants in constructors and domain methods

**Generic Example**:
```csharp
public class Order : AggregateRoot<Guid>
{
    public string OrderNumber { get; private set; }
    public OrderDetails OrderDetails { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; init; }

    // Constructor validates and initializes
    private Order() { }
    
    public Order(string orderNumber, OrderDetails orderDetails) 
        : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException(
                "Order number should not be null or empty", 
                nameof(orderNumber));
        
        OrderNumber = orderNumber;
        OrderDetails = orderDetails;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    // Domain behavior - encapsulated state changes
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Can only confirm pending orders");
        
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber));
    }
}
```

#### Value Objects

- Immutable objects representing domain concepts
- Implement equality based on values, not identity
- Use records for simple value objects
- Validate in constructors

```csharp
public record OrderDetails
{
    public required List<OrderLineItem> LineItems { get; init; }
    public required string ShippingAddress { get; init; }
    public required DateTime RequiredDeliveryDate { get; init; }
}
```

#### Domain Services

- Stateless services for complex business operations involving multiple aggregates
- Define interface in Domain layer, implement in Application layer
- Use for operations that don't naturally fit in an aggregate

```csharp
// Domain/Services/IOrderService.cs
public interface IOrderService
{
    Task<Order> CreateOrder(string orderNumber, OrderDetails orderDetails);
    Task<bool> ValidateOrderDetails(OrderDetails orderDetails);
    Task ConfirmOrder(Guid orderId);
}
```

#### Domain Events

- Capture business-significant state changes
- Use records for immutability
- Include correlation ID for distributed tracing
- Inherit from common event base interface

```csharp
public record OrderCreatedEvent(Guid OrderId, string OrderNumber, string CorrelationId) : IDomainEvent;

public record OrderConfirmedEvent(Guid OrderId, string OrderNumber) : IDomainEvent;
```

#### Domain Exceptions

- Create custom exceptions for domain-specific errors
- Include relevant context properties
- Inherit from appropriate base exception types

```csharp
public class InvalidOrderStatusTransitionException : DomainException
{
    public InvalidOrderStatusTransitionException(OrderStatus currentStatus, OrderStatus attemptedStatus) 
        : base($"Cannot transition from {currentStatus} to {attemptedStatus}")
    {
        CurrentStatus = currentStatus;
        AttemptedStatus = attemptedStatus;
    }
    
    public OrderStatus CurrentStatus { get; }
    public OrderStatus AttemptedStatus { get; }
}
```

### Application Layer

**Purpose**: Orchestrates domain operations, implements use cases via CQRS, and coordinates with infrastructure.

#### CQRS Commands

- Use records with primary constructors for immutability
- Inherit from `ICommand` or `IRequest<T>` (MediatR)
- Name clearly: `CreateOrder`, `ConfirmOrder`
- Include correlation ID for tracing

```csharp
public record CreateOrderCommand(string OrderNumber, OrderDetails Details, string CorrelationId) : ICommand<Guid>;

public record ConfirmOrderCommand(Guid OrderId, string CorrelationId) : ICommand;
```

#### Command Handlers

- Use primary constructors for dependency injection
- Implement MediatR's `IRequestHandler<TCommand>`
- Coordinate domain services and repositories
- Publish domain events
- Use structured logging

```csharp
public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IOrderService orderService,
    IEventPublisher eventPublisher,
    ILogger<CreateOrderCommandHandler> logger) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await orderService.CreateOrder(
            request.OrderNumber, 
            request.Details);
        
        await orderRepository.AddAsync(order, cancellationToken);
        
        var @event = new OrderCreatedEvent(order.Id, order.OrderNumber, request.CorrelationId);
        await eventPublisher.PublishAsync(@event, cancellationToken);
        
        logger.LogInformation(
            "[Create Order] Order {OrderId} has been created with CorrelationId {CorrelationId}", 
            order.Id,
            request.CorrelationId);
        
        return order.Id;
    }
}
```

#### Event Handlers

- Handle domain events and publish to message bus
- Keep handlers focused on single responsibility
- Use async operations
- Implement idempotency where needed

```csharp
public class OrderCreatedEventHandler(IMessagePublisher publisher) 
    : IEventHandler<OrderCreatedEvent>
{
    public async Task HandleAsync(
        OrderCreatedEvent @event, 
        CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(@event, cancellationToken);
    }
}
```

#### Application Services

- Implement domain service interfaces
- Orchestrate domain operations
- Handle cross-aggregate transactions
- Use repositories for persistence

```csharp
public class OrderService(
    IOrderRepository orderRepository, 
    IInventoryService inventoryService,
    ILogger<OrderService> logger) : IOrderService
{
    public async Task<Order> CreateOrder(
        string orderNumber, 
        OrderDetails orderDetails)
    {
        // Validate inventory availability
        var isAvailable = await inventoryService.ValidateAvailability(orderDetails.LineItems);
        if (!isAvailable)
            throw new InsufficientInventoryException("Required items not available");
        
        var order = new Order(orderNumber, orderDetails);
        await orderRepository.AddAsync(order);
        
        return order;
    }
}
```

#### Input Validation

- Validate DTOs and parameters in the application layer
- Use FluentValidation for complex validation rules
- Validate before executing domain logic
- Return problem details for validation errors

### Infrastructure Layer

**Purpose**: Provides technical capabilities for persistence, messaging, and external integrations.

#### Repositories

- Define interfaces in Domain layer
- Implement in Infrastructure layer
- Use EF Core or other ORMs
- Follow repository pattern for aggregate persistence

```csharp
// Domain/Repositories/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories/OrderRepository.cs
public class OrderRepository(ApplicationDbContext context) : IOrderRepository
{
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await context.Orders.AddAsync(order, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        context.Orders.Update(order);
        await context.SaveChangesAsync(cancellationToken);
    }
}
```

#### Message Bus Integration

- Abstract message bus behind interfaces
- Use dependency injection for producers/consumers
- Implement custom serializers for complex types
- Configure topics/queues in settings

```csharp
public static void AddMessaging(
    this IHostApplicationBuilder builder, 
    IConfiguration configuration)
{
    var messagingSettings = new MessagingSettings();
    configuration.GetSection("Messaging").Bind(messagingSettings);

    builder.Services.AddSingleton(messagingSettings);
    builder.Services.AddSingleton<IEventSerializer, JsonEventSerializer>();

    builder.AddKafkaProducer(messagingSettings);
    builder.Services.AddMediatR(cfg => 
        cfg.RegisterServicesFromAssemblyContaining<ApplicationAssemblyMarker>());

    builder.Services.AddScoped<IEventPublisher, EventPublisher>();
    builder.Services.AddScoped<INotificationPublisher, NotificationPublisher>();
}
```

#### Database Configuration

- Use .NET Aspire for connection management
- Configure DbContexts via extension methods
- Support multiple contexts (domain, outbox, etc.)
- Enable migrations in development

```csharp
public static void AddDatabase(
    this IHostApplicationBuilder builder, 
    IConfiguration configuration)
{
    builder.AddNpgsqlDbContext<OrderDbContext>("orderdb");
    builder.AddNpgsqlDbContext<OutboxDbContext>("orderdb");
    
    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
}
```

#### External Service Adapters

- Integrate with external systems through interfaces
- Implement circuit breaker patterns
- Handle retries and timeouts
- Log integration failures

### Presentation Layer (WebApi/Workers)

#### Minimal API Endpoints

- Group related endpoints
- Use proper HTTP status codes
- Apply authentication/authorization
- Return problem details for errors
- Document with OpenAPI

```csharp
public static class OrderEndpoints
{
    public static RouteGroupBuilder MapOrderEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateOrderAsync)
            .WithName("CreateOrder")
            .Produces<OrderResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem();
        
        group.MapPut("/{id:guid}/confirm", ConfirmOrderAsync)
            .WithName("ConfirmOrder")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
        
        return group;
    }

    private static async Task<IResult> CreateOrderAsync(
        CreateOrderRequest request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var command = new CreateOrderCommand(
            request.OrderNumber,
            request.Details,
            Guid.NewGuid().ToString());
        
        var orderId = await mediator.Send(command, cancellationToken);
        
        return Results.CreatedAtRoute("GetOrder", new { id = orderId }, new OrderResponse(orderId));
    }
}
```

#### Background Workers

- Inherit from `BackgroundService`
- Use scoped services via `IServiceProvider`
- Subscribe to message bus topics
- Handle messages with MediatR
- Implement graceful shutdown

```csharp
public class OrderProcessingWorker(
    ILogger<OrderProcessingWorker> logger,
    IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Order Processing Worker starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var consumer = scope.ServiceProvider.GetRequiredService<IOrderEventConsumer>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                
                var events = await consumer.ConsumeAsync(stoppingToken);
                foreach (var @event in events)
                {
                    await mediator.Publish(@event, stoppingToken);
                }
                
                await Task.Delay(1000, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing order events");
            }
        }

        logger.LogInformation("Order Processing Worker stopping");
    }
}
```

## Testing Standards

### Test Naming Convention

**MANDATORY**: Use `MethodName_Scenario_ExpectedResult` pattern

```csharp
[Fact]
public void Confirm_WhenOrderAlreadyConfirmed_ThrowsInvalidOperationException()
```

### Test Structure

- Follow AAA pattern (Arrange, Act, Assert)
- **DO NOT emit "Arrange", "Act", or "Assert" comments**
- Use FluentAssertions for expressive assertions
- One logical assertion per test

```csharp
[Fact]
public void Constructor_WithEmptyOrderNumber_ThrowsArgumentException()
{
    var orderDetails = new OrderDetails 
    { 
        LineItems = new List<OrderLineItem>(),
        ShippingAddress = "123 Main St",
        RequiredDeliveryDate = DateTime.UtcNow.AddDays(5)
    };

    var act = () => new Order("", orderDetails);

    act.Should().Throw<ArgumentException>()
        .WithMessage("*Order number should not be null or empty*");
}
```

### Domain Test Categories

- **Aggregate Tests**: Business rule validation and state changes
- **Value Object Tests**: Immutability and equality
- **Domain Service Tests**: Complex business operations
- **Event Tests**: Event publishing and handling
- **Application Service Tests**: Orchestration and input validation
- **Integration Tests**: Repository, message bus, and API endpoints

### Test Coverage

- Minimum **85%** for domain and application layers
- Test happy paths and edge cases
- Test exception scenarios
- Test state transitions in aggregates
- Test event publishing
- Use NSubstitute or Moq for mocking
- Use AutoFixture for test data generation

### Test Validation Process (MANDATORY)

Before writing any test, you MUST:

1. ✅ Verify naming follows pattern: `MethodName_Scenario_ExpectedResult`
2. ✅ Confirm test category: Unit/Integration/Acceptance
3. ✅ Check domain alignment: Test validates actual business rules
4. ✅ Review edge cases: Includes error scenarios and boundary conditions

## Implementation Guidelines

### Step 1: Domain Analysis (REQUIRED)

You MUST explicitly state:

- Domain concepts involved and their relationships (e.g., Order, Customer, Inventory)
- Aggregate boundaries and consistency requirements
- Ubiquitous language terms being used
- Business rules and invariants to enforce (e.g., orders cannot be confirmed without valid inventory)

### Step 2: Architecture Review (REQUIRED)

You MUST validate:

- How responsibilities are assigned to each layer
- Adherence to SOLID principles, especially SRP and DIP
- How domain events will be used for decoupling
- Security implications at the aggregate level
- Performance and scalability considerations

### Step 3: Implementation Planning (REQUIRED)

You MUST outline:

- Files to be created/modified with justification
- Test cases using `MethodName_Scenario_ExpectedResult` pattern
- Error handling and validation strategy
- Event publishing strategy
- Message flow through the system

### Step 4: Implementation Execution

1. Start with domain modeling and ubiquitous language
2. Define aggregate boundaries and consistency rules
3. Implement domain methods with proper encapsulation
4. Create domain events for business-significant changes
5. Implement command/query handlers with MediatR
6. Add event handlers for message bus integration
7. Configure infrastructure with extension methods
8. Implement application services with proper DI
9. Add comprehensive tests following naming conventions
10. Document domain decisions and trade-offs

### Step 5: Post-Implementation Review (REQUIRED)

You MUST verify:

- ✅ All quality checklist items are met
- ✅ Tests follow naming conventions and cover edge cases
- ✅ Domain rules are properly encapsulated in aggregates
- ✅ SOLID principles are followed
- ✅ Events are published correctly
- ✅ Message flow works as designed
- ✅ Logging is structured and meaningful
- ✅ Performance considerations are addressed

## Development Practices

### Event-First Design

- Model business processes as sequences of events (e.g., OrderCreated → OrderConfirmed → OrderShipped)
- Design aggregates to emit domain events
- Use events for cross-context communication
- Implement eventual consistency patterns

### Configuration Management

- Use strongly-typed configuration classes
- Bind configuration sections to POCOs
- Validate configuration on startup
- Use different settings per environment

```csharp
public class MessagingSettings
{
    public required string BrokerAddress { get; init; }
    public required string ConsumerGroup { get; init; }
    public required Dictionary<string, string> Topics { get; init; }
}

// In Program.cs or extension method
var messagingSettings = new MessagingSettings();
configuration.GetSection("Messaging").Bind(messagingSettings);
builder.Services.AddSingleton(messagingSettings);
```

### Dependency Injection Patterns

- Use constructor injection for all dependencies
- Register services via extension methods
- Use appropriate lifetimes (Scoped, Singleton, Transient)
- Avoid service locator anti-pattern

```csharp
public static class ConfigureDomainServices
{
    public static void AddDomainServices(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddSingleton<OrderValidator>();
    }
}
```

### Logging Best Practices

- Use structured logging with parameters
- Include correlation IDs for distributed tracing
- Log at appropriate levels
- Don't log sensitive data
- Use LoggerMessage source generators for high-performance logging

```csharp
logger.LogInformation(
    "[Create Order] Order {OrderId} has been created with CorrelationId {CorrelationId}",
    orderId,
    correlationId);
```

### Error Handling Strategy

- Use custom domain exceptions for business rule violations
- Implement global exception handling middleware
- Return problem details (RFC 7807) from APIs
- Log exceptions with context
- Don't catch exceptions you can't handle

## Quality Checklist

### Domain Design Validation

- ✅ "I have verified that aggregates properly model business concepts"
- ✅ "I have confirmed consistent terminology throughout the codebase"
- ✅ "I have verified the design follows SOLID principles"
- ✅ "I have validated that domain logic is encapsulated in aggregates"
- ✅ "I have confirmed domain events are properly published and handled"

### Implementation Quality Validation

- ✅ "I have written comprehensive tests following `MethodName_Scenario_ExpectedResult` naming"
- ✅ "I have considered performance implications and ensured efficient processing"
- ✅ "I have implemented proper error handling and validation"
- ✅ "I have documented domain decisions and architectural choices"
- ✅ "I have followed .NET best practices for async, DI, and error handling"

### Event-Driven Architecture Validation

- ✅ "I have verified events are published to correct topics"
- ✅ "I have ensured event handlers are idempotent"
- ✅ "I have implemented proper message serialization/deserialization"
- ✅ "I have added correlation IDs for distributed tracing"
- ✅ "I have tested message flow through the system"

### Infrastructure Validation

- ✅ "I have configured database contexts correctly"
- ✅ "I have implemented repositories following the interface contract"
- ✅ "I have configured message bus with proper settings"
- ✅ "I have enabled migrations for development"
- ✅ "I have used .NET Aspire for resource management"

**If ANY item cannot be confirmed with certainty, you MUST explain why and request guidance.**

## CRITICAL REMINDERS

**YOU MUST ALWAYS:**

- ✅ Show your thinking process before implementing
- ✅ Explicitly validate against these guidelines
- ✅ Use the mandatory verification statements
- ✅ Follow the `MethodName_Scenario_ExpectedResult` test naming pattern
- ✅ Confirm domain design aligns with DDD principles
- ✅ Stop and ask for clarification if any guideline is unclear   