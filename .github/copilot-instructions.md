# Copilot Instructions for .NET Testing Best Practices

## Project Overview

This repository demonstrates testing best practices for .NET applications following Domain-Driven Design (DDD) principles, SOLID architecture, and event-driven patterns. The focus is on practical, real-world testing examples using C# 13/.NET 9.

## Architecture Style

**DDD-First with Event-Driven Architecture**:
- **Domain Layer**: Aggregates, value objects, domain events - business logic lives here
- **Application Layer**: CQRS commands/queries with MediatR, orchestrates domain operations
- **Infrastructure Layer**: Repositories, message bus (Kafka/RabbitMQ), EF Core
- **Presentation Layer**: Minimal APIs and background workers

**Key Pattern**: Aggregates emit domain events → Event handlers publish to message bus → Background workers consume events

## Testing Standards

### Mandatory Test Naming Pattern

**ALWAYS use**: `MethodName_Scenario_ExpectedResult`

```csharp
[Fact]
public void Confirm_WhenOrderAlreadyConfirmed_ThrowsInvalidOperationException()

[Theory]
[InlineData("")]
[InlineData(null)]
public async Task CreateProduct_WithInvalidName_ThrowsArgumentException(string name)
```

### Test Structure Rules

- Follow AAA pattern (Arrange, Act, Assert)
- **NEVER emit "Arrange", "Act", or "Assert" comments** - structure should be self-evident
- Use FluentAssertions for assertions: `act.Should().Throw<ArgumentException>().WithMessage("*Order number*")`
- Test framework: xUnit
- Mocking: NSubstitute or Moq
- Test data: AutoFixture or Bogus

### Test Coverage Requirements

- **Minimum 85%** for domain and application layers
- Test categories:
  - **Domain Tests**: Aggregate state changes, business rules, invariants
  - **Application Tests**: Command/query handlers, orchestration
  - **Integration Tests**: Repositories, message bus, API endpoints using Testcontainers
  - **E2E Tests**: Complete workflows with WebApplicationFactory

## Critical Code Patterns

### Domain: Aggregate Example

```csharp
public class Order : AggregateRoot<Guid>
{
    public string OrderNumber { get; private set; }
    public OrderStatus Status { get; private set; }
    
    // Private parameterless constructor for EF Core
    private Order() { }
    
    // Public constructor validates invariants
    public Order(string orderNumber, OrderDetails orderDetails) : base(Guid.NewGuid())
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
            throw new ArgumentException("Order number should not be null or empty", nameof(orderNumber));
        
        OrderNumber = orderNumber;
        Status = OrderStatus.Pending;
    }
    
    // Behavior methods encapsulate state changes and emit events
    public void Confirm()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Can only confirm pending orders");
        
        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id, OrderNumber));
    }
}
```

**Key patterns**:
- Private setters enforce encapsulation
- Validate in constructor and domain methods
- Use primary constructors for value objects/services
- Domain events for business-significant changes

### Application: CQRS Command Handler

```csharp
public class CreateOrderCommandHandler(
    IOrderRepository orderRepository,
    IEventPublisher eventPublisher,
    ILogger<CreateOrderCommandHandler> logger) 
    : IRequestHandler<CreateOrderCommand, Guid>
{
    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new Order(request.OrderNumber, request.Details);
        await orderRepository.AddAsync(order, cancellationToken);
        
        var @event = new OrderCreatedEvent(order.Id, order.OrderNumber, request.CorrelationId);
        await eventPublisher.PublishAsync(@event, cancellationToken);
        
        logger.LogInformation(
            "[Create Order] Order {OrderId} created with CorrelationId {CorrelationId}", 
            order.Id, request.CorrelationId);
        
        return order.Id;
    }
}
```

**Key patterns**:
- Use primary constructors for DI
- Always include `CancellationToken`
- Publish domain events after persistence
- Structured logging with correlation IDs

## Code Quality Checklist

Before implementing, verify:

1. **Test naming**: Does it follow `MethodName_Scenario_ExpectedResult`?
2. **Domain encapsulation**: Are business rules in aggregates, not services?
3. **Event publishing**: Are domain events emitted for state changes?
4. **Null safety**: Using `is null`/`is not null` (NOT `== null`)?
5. **Async/await**: All I/O operations async with `CancellationToken`?
6. **SOLID adherence**: Single Responsibility and Dependency Inversion followed?

## Common Commands

```bash
# Run all tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/YourProject.UnitTests

# Verbose output
dotnet test --logger "console;verbosity=detailed"
```

## Testing Libraries Stack

- **xunit**: Test framework
- **FluentAssertions**: Expressive assertions
- **NSubstitute** or **Moq**: Mocking
- **AutoFixture**: Test data generation
- **Testcontainers**: Docker containers for integration tests
- **WireMock.Net**: HTTP mocking
- **WebApplicationFactory**: API testing

## Key Conventions

- **Namespaces**: File-scoped, PascalCase (e.g., `MyCompany.MyProduct.MyFeature`)
- **Async methods**: Always suffix with `Async`
- **Nullable types**: Enable everywhere (`<Nullable>enable</Nullable>`)
- **Private fields**: camelCase with `_` prefix (e.g., `_orderRepository`)
- **Configuration**: Strongly-typed classes with validation on startup
- **Logging**: Structured with parameters (NOT string concatenation)

## Important Guidelines References

- Full DDD/SOLID guidelines: [.github/instructions/dotnet-guidelines.instructions.md](.github/instructions/dotnet-guidelines.instructions.md)
- C# coding standards: [.github/instructions/csharp-guidelines.instructions.md](.github/instructions/csharp-guidelines.instructions.md)

## Integration Testing Pattern

```csharp
public class OrderRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    private ApplicationDbContext _context;
    
    // Use Testcontainers for isolated database
    public OrderRepositoryIntegrationTests()
    {
        _container = new PostgreSqlBuilder().Build();
    }
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Setup DbContext with container connection string
    }
    
    [Fact]
    public async Task AddAsync_WithValidOrder_PersistsToDatabase()
    {
        var repository = new OrderRepository(_context);
        var order = new Order("ORD-123", new OrderDetails { /* ... */ });
        
        await repository.AddAsync(order);
        
        var retrieved = await repository.GetByIdAsync(order.Id);
        retrieved.Should().NotBeNull();
        retrieved!.OrderNumber.Should().Be("ORD-123");
    }
    
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```

## Event-Driven Testing

Test event publishing and handling:

```csharp
[Fact]
public async Task Confirm_WhenOrderConfirmed_PublishesOrderConfirmedEvent()
{
    var order = new Order("ORD-123", orderDetails);
    
    order.Confirm();
    
    var domainEvents = order.GetDomainEvents();
    domainEvents.Should().ContainSingle()
        .Which.Should().BeOfType<OrderConfirmedEvent>()
        .Which.OrderId.Should().Be(order.Id);
}
```

## Anti-Patterns to Avoid

❌ **Don't**: Use "Arrange/Act/Assert" comments in tests  
❌ **Don't**: Use `== null` or `!= null` (use `is null`/`is not null`)  
❌ **Don't**: Block on async code with `.Result` or `.Wait()`  
❌ **Don't**: Put business logic in application layer (belongs in domain)  
❌ **Don't**: Generic test names like `Test1()` or `TestCreateOrder()`  
❌ **Don't**: Multiple assertions testing different scenarios (split into separate tests)

## Quick Reference

- **Target**: .NET 9, C# 13
- **Architecture**: DDD + CQRS + Event-Driven
- **Test naming**: `MethodName_Scenario_ExpectedResult`
- **No AAA comments**: Structure should be self-evident
- **Minimum coverage**: 85% for domain/application layers
- **Assertions**: FluentAssertions
- **Integration tests**: Use Testcontainers for isolation
