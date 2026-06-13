# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build CreditBackend.sln

# Run the API
dotnet run --project Src/Core/Credit.WebApi/Credit.WebApi.csproj

# Run all tests
dotnet test CreditBackend.sln

# Run tests in a specific project
dotnet test Src/Core/Credit.Domain/Credit.Domain.csproj

# EF Core migrations (run from Infrastructure project)
dotnet ef migrations add <MigrationName> --project Src/Core/Credit.Infrastructure --startup-project Src/Core/Credit.WebApi
dotnet ef database update --project Src/Core/Credit.Infrastructure --startup-project Src/Core/Credit.WebApi
```

## Architecture

This is a **.NET 9** credit management backend following **Clean Architecture** with **DDD** and **CQRS** patterns.

### Project Layers

```
Src/
  Core/
    Credit.Domain        — Aggregates, entities, value objects, domain events, domain services
    Credit.Application   — Use cases: commands, queries, handlers (via MediatR)
    Credit.Infrastructure — EF Core (PostgreSQL), MassTransit/RabbitMQ, repositories
    Credit.WebApi        — Minimal API endpoints, versioning, Program.cs
  Shared/
    SharedKernel         — Result<T>/Error pattern, shared messaging contracts
    SmartCore.Telemetry  — OpenTelemetry + Serilog configuration
```

### Key Patterns

**Result pattern** — All operations return `Result` or `Result<T>` from `SharedKernel`. Never throw for expected failures; use `Result.Failure(Error)`. Endpoints call `result.Match(onSuccess, onFailure)` and map failures via `CustomResults.Problem(error)`.

**CQRS via MediatR** — Commands implement `ICommand` or `ICommand<TResponse>`; queries implement `IQuery<TResponse>`. Handlers are registered by scanning the `Credit.Application` assembly in `Program.cs`.

**Minimal API endpoint registration** — Each feature group implements `IEndpoint` (single method `MapEndpoint`). All implementations are auto-discovered via reflection at startup and registered under `api/v{version}`. To add an endpoint, create a class implementing `IEndpoint` in `Credit.WebApi/Endpoints/`.

**Aggregate roots with domain events** — `AggregateRoot` (in `Credit.Domain/Abstractions/`) accumulates `IDomainEvent` instances via `Raise()`, which immediately calls `Apply()` to update state. The primary aggregate is `CreditContract`.

**Amortization strategy** — `AmortizationEngine` (domain service) selects a strategy by `CreditType`. Available strategies: French, German, American, Flat, Revolving. To add a strategy: implement `IAmortizationStrategy`, register as singleton in `Infrastructure/DependencyInjection.cs`.

### Infrastructure

- **Database**: PostgreSQL with EF Core. Connection string key: `DefaultConnection`. Schema uses `uuid-ossp` extension; all columns are snake_case. The `CreditDbContext` is scaffolded — entity configurations live in `OnModelCreating`.
- **Messaging**: MassTransit over RabbitMQ. Config key: `RabbitMqSettings:Uri`. Consumers live in `Credit.Infrastructure/Messaging/Consumers/`. Shared message contracts are in `SharedKernel/Contracts/`.
- **Telemetry**: `SmartCore.Telemetry` provides OpenTelemetry (ASP.NET Core, EF Core, HTTP, Redis, Runtime) and Serilog sinks. Configure via `TelemetryOptions`.
- **API versioning**: URL segment (`api/v1/...`). Default version is `1`. Add new versions with `new ApiVersion(N)` in the version set and per endpoint.

### Domain Model

Core entities (persisted via EF Core): `Customer`, `CreditProduct`, `CreditApplication`, `CreditLine`, `CreditPayment`, `Installment`, `DomainEvent`.

Lifecycle: `CreditApplication` (pending → approved/rejected) → `CreditLine` (active credit with amortization schedule and installments) → `CreditPayment` (payments recorded against a line).
