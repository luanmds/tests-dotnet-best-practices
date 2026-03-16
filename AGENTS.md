
# AGENTS.md

# Project Information

## Overview
Demonstrates .NET testing best practices using DDD, CQRS, SOLID, and event-driven architecture.

## Architecture
- **Domain:** Aggregates, value objects, domain events, business invariants
- **Application:** Command/query handlers, orchestration logic
- **Infrastructure:** Data persistence, repositories, messaging
- **API/Worker:** HTTP endpoints, background consumers

## Technology Stack
- .NET 9 / C# 13
- MediatR (CQRS)
- Entity Framework Core
- RabbitMQ-style messaging
- xUnit, FluentAssertions, NSubstitute/Moq
- Testcontainers, WebApplicationFactory

## Testing Approach
- Unit tests: domain & application logic
- Integration tests: repositories, messaging, API
- End-to-end: API & worker flows

## Repository Structure
- `src/PointsWallet.Domain`: Domain model, business rules
- `src/PointsWallet.Infrastructure`: EF Core, repositories, messaging
- `src/PointsWallet.Api`: Minimal API endpoints
- `src/PointsWallet.Worker`: Background processing
- `tests/PointsWallet.UnitTests`: Unit tests
- `tests/PointsWallet.IntegrationTests`: Integration tests

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
- Follow project structure and naming conventions

### Workflow Orchestration (Agent Guidance)
1. **Plan Mode Default:** For any non-trivial task, enter plan mode and write specs before coding.
2. **Subagent Strategy:** Use subagents for research, exploration, and parallel analysis.
3. **Verification Before Done:** Never mark a task complete without running all tests and verifying behavior.
4. **Demand Elegance:** Seek the most maintainable solution for non-trivial changes.
5. **Autonomous Bug Fixing:** When a bug is reported, fix it and verify with tests—no hand-holding required.

---

## References
- [README.md](README.md): Full setup, test, and troubleshooting instructions
- [.github/instructions/csharp-guidelines.instructions.md](.github/instructions/csharp-guidelines.instructions.md): C# code conventions
- [.github/instructions/test-guidelines.instructions.md](.github/instructions/test-guidelines.instructions.md): Test best practices

# End of AGENTS.md
