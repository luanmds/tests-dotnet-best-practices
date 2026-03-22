# .NET Testing Best Practices
A comprehensive guide to writing effective, maintainable, and reliable tests in .NET applications.

## Overview

This repository provides best practices, patterns, and guidelines for testing .NET applications, including integration and end-to-end tests. The goal is to help developers write tests that are:

- **Readable**: Easy to understand and maintain
- **Reliable**: Consistent and deterministic results
- **Fast**: Quick execution for rapid feedback
- **Maintainable**: Easy to update as the codebase evolves

## Table of Contents

- [.NET Testing Best Practices](#net-testing-best-practices)
  - [Overview](#overview)
  - [Table of Contents](#table-of-contents)
  - [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
  - [Testing Frameworks](#testing-frameworks)
  - [Best Practices](#best-practices)
  - [Project Organization](#project-organization)
    - [Project Structure](#project-structure)
      - [Test Types](#test-types)
    - [Test Categories](#test-categories)
  - [Tools and Libraries](#tools-and-libraries)
    - [Recommended Packages \& Tools](#recommended-packages--tools)
      - [Test Infrastructure](#test-infrastructure)
  - [Running Integration Tests](#running-integration-tests)
    - [Prerequisites](#prerequisites-1)
    - [Setup Environment Variables](#setup-environment-variables)
    - [Setup User Secrets](#setup-user-secrets)
    - [Running Tests](#running-tests)
    - [Using Docker Compose for Local Development](#using-docker-compose-for-local-development)
    - [Integration Tests Architecture](#integration-tests-architecture)

## Getting Started

### Prerequisites

- .NET 9.0 or later
- Basic understanding of C# and .NET

## Testing Frameworks

This repository will demonstrate testing approaches using modern .NET testing frameworks and libraries. The focus is on practical, real-world examples that can be applied to your projects.

## Best Practices

This repository demonstrates key testing principles:

- **Test Isolation**: Each test runs in a clean, isolated environment (using Testcontainers or Aspire for integration tests)
- **Clarity**: Tests are easy to read, maintain, and reason about
- **Speed**: Fast feedback via parallelizable, containerized tests
- **Reliability**: Consistent, deterministic results across environments
- **Coverage**: Comprehensive coverage of business logic and integration points
- **Maintainability**: Tests are easy to update as the codebase evolves
- **Product Realism Based**: Integration tests use real infrastructure (DB, messaging) for realistic scenarios and confidence in production readiness

## Project Organization

### Project Structure

```
Solution/
├── src/
│   ├── PointsWallet.Api/                # Minimal API, endpoints, DI
│   ├── PointsWallet.Domain/             # Domain models, commands, validators
│   ├── PointsWallet.Infrastructure/     # EF Core, repositories, messaging
│   └── PointsWallet.Worker/             # Background processing
└── tests/
  ├── PointsWallet.UnitTests/          # Unit tests (domain/application logic)
  ├── PointsWallet.IntegrationTests/   # Integration tests (real DB, messaging, API)
  └── PointsWallet.AspireIntegrationTests/ # Aspire-powered distributed integration tests
```

#### Test Types
- **Unit Tests**: [WIP] Pure C# logic, no infrastructure dependencies
- **Integration Tests**: Real database (PostgreSQL), messaging (RabbitMQ), and API using Testcontainers
- **Aspire Integration Tests**: Distributed scenarios orchestrated with .NET Aspire

### Test Categories

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test interactions between components
- **End-to-End Tests**: Test complete workflows through the system

## Tools and Libraries

### Recommended Packages & Tools

- `xunit` – Test framework
- `FluentAssertions` – Expressive assertions
- `Moq` or `NSubstitute` – Mocking
- `AutoFixture`, `Bogus` – Test data generation
- `Testcontainers` – Dockerized PostgreSQL/RabbitMQ for integration tests
- `Aspire.Hosting.Testing` – Orchestrate distributed app scenarios in Aspire integration tests
- `WebApplicationFactory` – In-memory API hosting for integration tests

#### Test Infrastructure
- **Testcontainers**: Used in `PointsWallet.IntegrationTests` project to spin up real PostgreSQL and RabbitMQ containers for each test class, ensuring isolation and reproducibility. No manual DB setup required; supports parallel test execution.
- **Aspire**: Used in `PointsWallet.AspireIntegrationTests` project to orchestrate distributed app components and dependencies for advanced integration scenarios. Provides fixtures for spinning up the app host and exposing a pre-configured `HttpClient` for API tests.


## Running Integration Tests

### Prerequisites

- .NET 9 SDK
- Docker (for Testcontainers and Aspire)
- PostgreSQL (optional, for local development)
- RabbitMQ (optional, for local development)

### Setup Environment Variables

Set up the PostgreSQL password as an environment variable for better security:

```bash
# Linux/macOS - Add to ~/.bashrc or ~/.zshrc for persistence
export POSTGRES_PASSWORD="your_secure_password_here"

# Windows PowerShell
$env:POSTGRES_PASSWORD="your_secure_password_here"

# Windows Command Prompt
set POSTGRES_PASSWORD=your_secure_password_here
```

### Setup User Secrets

The integration tests require database connection strings. Configure them using .NET User Secrets with environment variable references:

```bash
# Navigate to the API project directory
cd src/PointsWallet.Api

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set the connection string using environment variable for password
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=pointswalletdb;Username=pointswallet;Password=${POSTGRES_PASSWORD}"
```

Alternatively, for the test project:

```bash
# Navigate to the integration tests project
cd tests/PointsWallet.IntegrationTests

# Initialize user secrets
dotnet user-secrets init

# Set the connection string using environment variable for password
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=pointswalletdb_test;Username=pointswallet;Password=${POSTGRES_PASSWORD}"
```

**Note**: For Docker Compose, the password is defined in the `docker-compose.yml` file. Consider using `.env` file for environment-specific values:

```bash
# Create .env file in project root (add to .gitignore!)
echo "POSTGRES_PASSWORD=your_secure_password_here" > .env
```

Then update `docker-compose.yml` to reference it:
```yaml
environment:
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
```

### Running Tests

```bash
# Run all tests (unit + integration)
dotnet test

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run with detailed logging
dotnet test --logger "console;verbosity=detailed"

# Run tests in a specific project
dotnet test tests/PointsWallet.IntegrationTests
```

### Using Docker Compose for Local Development

Start the PostgreSQL database using environment variables:

```bash
# Create .env file in project root (add to .gitignore!)
cat > .env << EOF
POSTGRES_PASSWORD=your_secure_password_here
POSTGRES_USER=pointswallet
POSTGRES_DB=pointswalletdb
EOF

# Start database
docker-compose up postgres -d

# Verify database is healthy
docker-compose ps

# Stop database
docker-compose down
```

**Security Note**: Make sure to add `.env` to your `.gitignore` file to prevent committing sensitive credentials:

```bash
# Add to .gitignore
echo ".env" >> .gitignore
```

### Integration Tests Architecture

Integration tests use **Testcontainers** to spin up isolated PostgreSQL and RabbitMQ containers, ensuring:
- ✅ Test isolation (each test class gets a fresh database and message broker)
- ✅ No manual database or broker setup required
- ✅ Consistent behavior across environments
- ✅ Parallel test execution support

Aspire integration tests use **.NET Aspire** to orchestrate distributed app scenarios, spinning up the app host and dependencies for realistic, end-to-end testing of workflows and service interactions.

Example integration test structure:

```csharp
public class WalletRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;
    
    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        // Setup DbContext with container connection string
    }
    
    [Fact]
    public async Task AddAsync_WithValidWallet_PersistsToDatabase()
    {
        // Test implementation
    }
    
    public async Task DisposeAsync() => await _container.DisposeAsync();
}
```
