---
description: '.NET testing patterns and best practices'
applyTo: '**/*Tests.cs,**/tests/**'
---

# .NET Testing Patterns and Best Practices

Use this file as the canonical source for testing standards across this repository.

## Universal Standards

### Test Naming (MANDATORY)

Use the pattern `MethodName_Scenario_ExpectedResult`.

Examples:

```csharp
[Fact]
public void Confirm_WhenOrderAlreadyConfirmed_ThrowsInvalidOperationException()

[Theory]
[InlineData("")]
[InlineData(null)]
public async Task CreateProduct_WithInvalidName_ThrowsArgumentException(string? name)
```

### Test Structure

- Follow AAA flow (Arrange, Act, Assert)
- Do not emit "Arrange", "Act", or "Assert" comments
- Keep each test focused on one business concern
- Prefer FluentAssertions for expressive assertions

```csharp
[Fact]
public void Constructor_WithEmptyOrderNumber_ThrowsArgumentException()
{
    var details = new OrderDetails
    {
        LineItems = [],
        ShippingAddress = "123 Main St",
        RequiredDeliveryDate = DateTime.UtcNow.AddDays(5)
    };

    var act = () => new Order("", details);

    act.Should().Throw<ArgumentException>()
        .WithMessage("*Order number should not be null or empty*");
}
```

### Assertions and Frameworks

- Test framework: xUnit
- Assertions: FluentAssertions
- Mocking: NSubstitute or Moq
- Test data: AutoFixture or Bogus

### Coverage and Scope

- Minimum 85% coverage for domain and application layers
- Cover happy paths, edge cases, and exception flows
- Validate aggregate state transitions and event publishing

## Unit Test Patterns

### Purpose

- Validate business logic in isolation
- Ensure fast and deterministic execution

### Isolation Rules

- Mock all external dependencies (database, network, file system, message bus)
- Never call real infrastructure from unit tests
- Avoid fixed-time or random behavior without control

### Unit Test Checklist

1. Uses `MethodName_Scenario_ExpectedResult`
2. Focuses on one concern
3. Verifies expected behavior and error handling
4. Avoids environment dependencies

## Integration Test Patterns

### Purpose

- Validate end-to-end behavior across boundaries (API, DB, messaging)
- Confirm wiring and infrastructure contracts

### Infrastructure Rules

- Prefer `WebApplicationFactory` for API-level integration tests
- Use Testcontainers for isolated dependencies
- Use fixture lifecycle (`IAsyncLifetime`) for startup/cleanup
- Keep test data deterministic and isolated per scenario

### Integration Test Checklist

1. Covers a real cross-layer flow
2. Asserts observable behavior (status, payload, persisted state, emitted message)
3. Cleans up resources and test data
4. Avoids shared mutable state between tests

## Domain-Oriented Test Categories

- Aggregate tests: invariant enforcement and state changes
- Value object tests: immutability and equality
- Domain service tests: multi-aggregate business rules
- Event tests: publication and handler behavior
- Application tests: handler orchestration and validation behavior
- Integration tests: repository/message/API contracts

## Test Commands

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

## Anti-Patterns

- Generic names such as `Test1` or `TestCreateOrder`
- AAA comments in test methods
- Blocking async calls with `.Result` or `.Wait()`
- Tests with unrelated assertions for different scenarios
- Unit tests coupled to real infrastructure

## Validation Process (MANDATORY)

Before writing tests, verify:

1. Naming follows `MethodName_Scenario_ExpectedResult`
2. Category is clear (Unit/Integration/Acceptance)
3. Business rule coverage is explicit
4. Edge and error cases are included
