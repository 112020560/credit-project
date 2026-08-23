# CLAUDE.md (Crm)

This file provides guidance to Claude Code when working under `Src/Crm/`. It applies **only** to the Crm subproject — the root `CLAUDE.md` covers monorepo-wide conventions (Core, Gateway, OpenSpec workflow). Where the two disagree, this file wins for code under `Src/Crm/`.

> **Note**: Crm uses **EF Core**, unlike `CreditSystem.*` (Core), which uses Dapper + raw Npgsql exclusively. Do not apply the root's "no EF Core" rule to this subtree.

## Commands

```bash
# Build the whole monorepo (includes Crm)
dotnet build CreditBackend.sln

# Run the Crm API
dotnet run --project Src/Crm/Crm.WebApi/Crm.WebApi.csproj
```

**EF Core migrations** (run from `Src/Crm/Crm.WebApi` or `Src/Crm/Crm.Infrastructure`):
```bash
dotnet ef migrations add <MigrationName> --project ../Crm.Infrastructure --startup-project .
dotnet ef database update --project ../Crm.Infrastructure --startup-project .
```

## Architecture

.NET 9 Web API following **Clean Architecture** with four layers:

```
Crm.Domain         → Entities, domain abstractions (ICustomersRepository, IUnitOfWork)
Crm.Application    → Use cases via MediatR (Commands/Queries), DTOs, FluentValidation
Crm.Infrastructure → EF Core (PostgreSQL), MassTransit/RabbitMQ implementations
Crm.WebApi         → Minimal API endpoints, DI wiring, middleware
```

### Request flow

HTTP request → `IEndpoint` (Minimal API) → `IMediator.Send()` → MediatR pipeline → `ICommandHandler` / `IQueryHandler` → Repository via `IUnitOfWork` → PostgreSQL

MediatR pipeline behaviors (applied in order):
1. `RequestLoggingPipelineBehavior` — logs every request
2. `ValidationPipelineBehavior` — runs FluentValidation; returns `Result.ValidationFailure` instead of throwing

### Key patterns

- **Result type**: All handlers return `Result<T>` or `Result` from the `SharedKernel` NuGet package. Endpoints call `.Match()` to produce `IResult`.
- **Endpoints**: Each endpoint class implements `IEndpoint` and is auto-discovered via reflection at startup (`AddEndpoints` / `MapEndpoints` in `EndpointExtensions.cs`). All routes are versioned under `api/v{version}`. This differs from Core, which registers endpoint groups explicitly in `Program.cs` with no reflection and no URL versioning.
- **Commands vs Queries**: Commands implement `ICommand<TResponse>` (wraps `IRequest<Result<TResponse>>`); queries implement `IQuery<TResponse>`.
- **Messaging**: After mutating state, commands publish to RabbitMQ via `IMqProducerService`. Two patterns are used: `SendCommand` (point-to-point to a named queue) and `PublishEvent` (broadcast). MassTransit wraps RabbitMQ.
- **Domain entities**: `Customer` is a partial class split across `Customer.cs` (properties + `ConvertToModel()`) and EF configuration in `CrmDbContext.cs`. Domain entities are mapped directly to DB tables (no separate EF models).
- **Telemetry**: OpenTelemetry via the internal `SmartCore.Telemetry` NuGet package, exporting to OTLP. Serilog is used for structured logging (Console + Seq in development).

### Infrastructure dependencies

- **Database**: PostgreSQL via Npgsql EF Core. Connection string key: `ConnectionStrings:DefaultConnection`.
- **Message broker**: RabbitMQ. Connection URI key: `RabbitMqSettings:Uri`.
- **Logging**: Seq at `http://localhost:5341` in development.
- **Tracing**: OTLP endpoint at `Telemetry:OtlpEndpoint` (default `http://localhost:4317`).

### Configuration

Secrets and environment-specific values go in `appsettings.Development.json` (not committed in production). Required config keys:
- `ConnectionStrings:DefaultConnection`
- `RabbitMqSettings:Uri`
- `Jwt:Secret`, `Jwt:Issuer`, `Jwt:Audience`

## OpenSpec

Crm's own OpenSpec capabilities (`prospect-management`, `customer-management`, `risk-evaluation`, `risk-rules`, `document-management`, etc.) now live alongside Core's in the monorepo's `openspec/specs/` and `openspec/changes/` — follow the same `opsx:*` workflow described in the root `CLAUDE.md`.
