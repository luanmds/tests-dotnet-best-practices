
# AGENTS.md

# Project Information

## Overview
Demonstrates .NET testing best practices using DDD, CQRS, SOLID, and event-driven architecture. The repository covers unit, integration, and distributed (Aspire) integration tests, using modern .NET tooling and containerized infrastructure.

## Architecture
- **Domain:** Aggregates, value objects, domain events, business invariants
- **Application:** Command/query handlers, orchestration logic
- **Infrastructure:** Data persistence, repositories, messaging
- **API/Worker:** HTTP endpoints, background consumers
- **Testing:**
	- **Unit:** Pure C# logic, no infrastructure
	- **Integration:** Real DB/messaging via Testcontainers
	- **Aspire Integration:** Distributed scenarios orchestrated with .NET Aspire

## Technology Stack
- .NET 9 / C# 13
- MediatR (CQRS)
- Entity Framework Core
- RabbitMQ-style messaging
- xUnit, FluentAssertions, NSubstitute/Moq
- Testcontainers (for PostgreSQL, RabbitMQ in integration tests)
- Aspire.Hosting.Testing (for distributed integration tests)
- WebApplicationFactory (in-memory API hosting)

## Testing Approach
- **Unit tests:** Domain & application logic (pure C#)
- **Integration tests:** Repositories, messaging, API (using Testcontainers for real DB and messaging)
- **Aspire integration tests:** Distributed scenarios using .NET Aspire for orchestration and realistic end-to-end flows

## Repository Structure
- `src/PointsWallet.Domain`: Domain model, business rules
- `src/PointsWallet.Infrastructure`: EF Core, repositories, messaging
- `src/PointsWallet.Api`: Minimal API endpoints
- `src/PointsWallet.Worker`: Background processing
- `tests/PointsWallet.UnitTests`: Unit tests (domain/application logic)
- `tests/PointsWallet.IntegrationTests`: Integration tests (real DB, messaging, API with Testcontainers)
- `tests/PointsWallet.AspireIntegrationTests`: Aspire-powered distributed integration tests

---

## How to Run This Project

### Prerequisites
- .NET 9 SDK
- Docker (for Testcontainers)
- PostgreSQL (local or via Docker)

### Setup
1. Clone the repository.
2. Set environment variables (e.g., `POSTGRES_PASSWORD`).
3. (Optional) Use Docker Compose for local PostgreSQL:

   ```bash
   # Create .env file (add to .gitignore!)
   echo "POSTGRES_PASSWORD=your_secure_password_here" > .env
   docker-compose up postgres -d
   ```
4. Configure user secrets for connection strings:
	 ```bash
	 cd src/PointsWallet.Api
	 dotnet user-secrets init
	 dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=pointswalletdb;Username=pointswallet;Password=${POSTGRES_PASSWORD}"
	 ```

### Running Tests
- Run all tests:
	```bash
	dotnet test
	```
- Run only integration tests:
	```bash
	dotnet test --filter "Category=Integration"
	```
- Run Aspire integration tests:
	```bash
	dotnet test tests/PointsWallet.AspireIntegrationTests
	```
- Run with code coverage:
	```bash
	dotnet test --collect:"XPlat Code Coverage"
	```

---

## .NET/C# Project Guidance

### Core Principles
- **Simplicity First:** Minimal, clear changes
- **No Laziness:** Find root causes, avoid workarounds
- **Minimal Impact:** Only touch what's necessary

### Code & Test Conventions
- Use DDD, CQRS, SOLID, and event-driven patterns
- Write isolated, reliable, and fast tests
- Use xUnit, FluentAssertions, NSubstitute/Moq
- Prefer integration tests with Testcontainers for DB/API
- Use Aspire for distributed integration scenarios
- Follow project structure and naming conventions

### Workflow Orchestration (Agent Guidance)
1. **Plan Mode Default:** For any non-trivial task, enter plan mode and write specs before coding.
2. **Subagent Strategy:** Use subagents for research, exploration, and parallel analysis.
3. **Verification Before Done:** Never mark a task complete without running all tests and verifying behavior.
4. **Demand Elegance:** Seek the most maintainable solution for non-trivial changes.
5. **Autonomous Bug Fixing:** When a bug is reported, fix it and verify with tests—no hand-holding required.

---

## Notes on Testcontainers & Aspire
- **Testcontainers**: Used for PostgreSQL and RabbitMQ in integration tests, providing isolated, reproducible environments for each test class.
- **Aspire**: Used in Aspire integration tests to orchestrate distributed app scenarios, spinning up the app host and dependencies for realistic, end-to-end testing.

---

## References
- [README.md](README.md): Full setup, test, and troubleshooting instructions
- [.github/instructions/csharp-guidelines.instructions.md](.github/instructions/csharp-guidelines.instructions.md): C# code conventions
- [.github/instructions/test-guidelines.instructions.md](.github/instructions/test-guidelines.instructions.md): Test best practices

# End of AGENTS.md
