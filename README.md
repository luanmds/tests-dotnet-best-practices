# .NET Testing Best Practices

A comprehensive guide to writing effective, maintainable, and reliable tests in .NET applications.

## Overview

This repository provides best practices, patterns, and guidelines for testing .NET applications, including integration tests and end-to-end tests. The goal is to help developers write tests that are:

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
  - [Test Organization](#test-organization)
    - [Project Structure](#project-structure)
    - [Test Categories](#test-categories)
  - [Tools and Libraries](#tools-and-libraries)
    - [Recommended Packages](#recommended-packages)
  - [Running Integration Tests](#running-integration-tests)
    - [Prerequisites](#prerequisites-1)
    - [Setup User Secrets](#setup-user-secrets)
    - [Running Tests](#running-tests)
    - [Using Docker Compose for Local Development](#using-docker-compose-for-local-development)
    - [Integration Tests Architecture](#integration-tests-architecture)

## Getting Started

### Prerequisites

- .NET 9.0 or later
- Visual Studio 2026, VS Code, or Rider
- Basic understanding of C# and .NET

## Testing Frameworks

This repository will demonstrate testing approaches using modern .NET testing frameworks and libraries. The focus is on practical, real-world examples that can be applied to your projects.

## Best Practices

This repository demonstrates key testing principles:

- **Test Isolation**: Tests should not depend on each other or share state
- **Clarity**: Tests should be easy to understand and maintain
- **Speed**: Fast execution for rapid feedback during development
- **Reliability**: Consistent and deterministic test results
- **Coverage**: Appropriate test coverage for critical business logic

## Test Organization

### Project Structure

```
Solution/
├── src/
│   ├── PointsWallet.Api/           # Minimal API, endpoints, DI
│   ├── PointsWallet.Domain/        # Domain models, commands, validators
│   └── PointsWallet.Infrastructure/# EF Core, repositories, DI
└── tests/
    ├── PointsWallet.UnitTests/     # Unit tests for domain/application
    ├── PointsWallet.IntegrationTests/ # Integration tests (e.g. DB, API)
```

### Test Categories

- **Unit Tests**: Test individual components in isolation
- **Integration Tests**: Test interactions between components
- **End-to-End Tests**: Test complete workflows through the system

## Tools and Libraries

### Recommended Packages

- `xunit` - Testing framework
- `FluentAssertions` - Assertion library
- `Moq` or `NSubstitute` - Mocking framework
- `AutoFixture` - Test data generation
- `Bogus` - Fake data generator
- `Testcontainers` - Docker containers for integration tests
- `WireMock.Net` - HTTP mocking
- `Respawn` - Database cleanup
The repository will be organized to demonstrate different types of tests:

- **Unit Tests**: Testing individual components in isolation
- **Integration Tests**: Testing interactions between components and external dependencies
- **End-to-End Tests**: Testing complete workflows through the system

Each test category serves a specific purpose and helps ensure comprehensive coverage of the application.

// ...existing code...

## Running Integration Tests

### Prerequisites

- .NET 9 SDK
- Docker (for Testcontainers)
- PostgreSQL (optional, for local development)

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

### Troubleshooting

**Environment variable not found:**
```bash
# Verify environment variable is set
echo $POSTGRES_PASSWORD  # Linux/macOS
echo %POSTGRES_PASSWORD%  # Windows CMD
echo $env:POSTGRES_PASSWORD  # Windows PowerShell

# If not set, export it again
export POSTGRES_PASSWORD="your_secure_password_here"
```

**Docker not available:**
```bash
# Verify Docker is running
docker ps

# On Linux, ensure user is in docker group
sudo usermod -aG docker $USER
newgrp docker
```

**Port conflicts:**
```bash
# Check if port 5432 is in use
sudo lsof -i :5432

# Stop existing PostgreSQL service
sudo systemctl stop postgresql
```

**Testcontainers issues:**
```bash
# Clean up dangling containers
docker system prune -f

# Remove test containers
docker ps -a | grep testcontainers | awk '{print $1}' | xargs docker rm -f
```

### Integration Tests Architecture

Integration tests use **Testcontainers** to spin up isolated PostgreSQL instances, ensuring:
- ✅ Test isolation (each test class gets a fresh database)
- ✅ No manual database setup required
- ✅ Consistent behavior across environments
- ✅ Parallel test execution support

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
