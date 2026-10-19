# DentFlow API — Agent Reference

ASP.NET Core 9, FastEndpoints, CQRS (MediatR), EF Core 9 + PostgreSQL, Clean Architecture + Modular Monolith.

## Tech Stack

| Concern         | Library                          |
|-----------------|----------------------------------|
| HTTP layer      | FastEndpoints 5.x                |
| CQRS dispatch   | MediatR 14.x                     |
| Validation      | FluentValidation 12.x            |
| Error handling  | ErrorOr 2.x                      |
| ORM             | EF Core 9 + Npgsql               |
| Multi-tenancy   | Finbuckle.MultiTenant 9.x        |
| Auth            | ASP.NET Core Identity + JWT      |
| Background jobs | Hangfire + PostgreSQL storage    |
| Caching         | StackExchange.Redis 2.x          |
| Logging         | Serilog 9.x                      |
| Testing         | xUnit + NSubstitute + FluentAssertions |

## Solution Structure

```
dentflow-api/
├── src/
│   ├── DentFlow.API/            # Program.cs, middleware, endpoint registration
│   ├── DentFlow.Application/    # MediatR pipeline behaviors, shared interfaces
│   ├── DentFlow.Domain/         # Base entities, value objects, shared errors
│   ├── DentFlow.Infrastructure/ # DbContext, repositories, external services
│   └── Modules/
│       ├── DentFlow.{Module}/   # Each feature module (see Module Structure below)
│       └── ...
├── tests/
│   ├── DentFlow.{Module}.Tests/
│   └── http/                    # .http test files per module
└── documentation/
```

**Dependency rule (strictly enforced):** `API → Modules → Application → Domain`. Infrastructure implements Application interfaces. Domain has zero external dependencies.

## Module Structure

Every new feature follows this exact layout:

```
Modules/DentFlow.{ModuleName}/
├── Domain/
│   ├── {Entity}.cs              # Inherits TenantAuditableEntity
│   └── {Entity}Errors.cs       # Typed ErrorOr errors
├── Application/
│   ├── Commands/
│   │   ├── Create{Entity}Command.cs
│   │   ├── Create{Entity}CommandHandler.cs  # Returns ErrorOr<T>
│   │   └── Create{Entity}CommandValidator.cs
│   └── Queries/
│       ├── Get{Entity}ByIdQuery.cs
│       └── Get{Entity}ByIdQueryHandler.cs
├── Endpoints/
│   ├── {Entity}CreateEndpoint.cs
│   └── {Entity}GetByIdEndpoint.cs
└── DentFlow.{ModuleName}.csproj
```

## Common Commands

```bash
# Run API (from dentflow-api/)
dotnet run --project src/DentFlow.API

# Run all tests
dotnet test

# Run tests for one module
dotnet test tests/DentFlow.Patients.Tests/

# Add EF migration (run from dentflow-api/)
dotnet ef migrations add {MigrationName} \
  --project src/DentFlow.Infrastructure \
  --startup-project src/DentFlow.API

# Apply migrations
dotnet ef database update \
  --project src/DentFlow.Infrastructure \
  --startup-project src/DentFlow.API

# Build solution
dotnet build DentFlow.sln
```

## Base Classes — Always Inherit These

```csharp
// Tenant-scoped entity (99% of domain entities)
public class Patient : TenantAuditableEntity { }
// Gives: Id (Guid), TenantId, CreatedAt, UpdatedAt, CreatedBy, IsDeleted, DeletedAt

// Non-tenant entity (SuperAdmin-only, e.g. Tenant itself)
public class Tenant : BaseEntity { }
// Gives: Id (Guid), CreatedAt, UpdatedAt
```

## Database Conventions

- **PKs**: `Guid` (never `int` identity)
- **Money**: `decimal` mapped to `NUMERIC(10,2)`
- **Timestamps**: `DateTime` always UTC, column type `TIMESTAMPTZ`
- **Soft deletes**: Set `IsDeleted = true` + `DeletedAt` — never `DELETE FROM`
- **Naming**: snake_case tables, set explicitly per entity via `.ToTable("snake_case_name")` in `ApplicationDbContext` / the `Persistence/Configurations/` classes. There is **no** `UseSnakeCaseNamingConvention()` call — new entities must declare their table name themselves
- **Tenant scoping**: Automatic via EF global query filters — never add manual `WHERE tenant_id = ...`. ⚠️ The filter mechanism works, but tenant *resolution* currently collapses to `Guid.Empty` (see root `CLAUDE.md` divergences, Faza 1.1)
- **No raw SQL** for business logic — use EF Core; raw SQL only for reporting queries

## CQRS Pattern

```csharp
// Handler always returns ErrorOr<T>
public class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(CreatePatientCommand request, CancellationToken ct)
    {
        // validate domain rules, return Error or Success
        return new PatientResponse(...);
    }
}

// Endpoint maps HTTP → MediatR, never contains business logic
public class PatientCreateEndpoint : Endpoint<CreatePatientRequest, PatientResponse>
{
    public override async Task HandleAsync(CreatePatientRequest req, CancellationToken ct)
    {
        var result = await mediator.Send(req.ToCommand());
        await result.MatchAsync(
            onValue: async p => await SendOkAsync(p, ct),
            onError: async errors => await SendErrorsAsync(errors, ct));
    }
}
```

## Error Handling

```csharp
// Define typed errors in Domain/{Entity}Errors.cs
public static class PatientErrors
{
    public static readonly Error NotFound = Error.NotFound("Patient.NotFound", "Patient not found.");
    public static readonly Error DuplicateMrn = Error.Conflict("Patient.DuplicateMrn", "MRN already in use.");
}

// Use in handler
if (patient is null) return PatientErrors.NotFound;
```

## Authentication & Authorization

- JWT Bearer. Access token expiry: 15 min in production, 60 min in Development
- **No refresh token flow exists** — the `RefreshToken` entity and `refresh_tokens` table are unused, and there is no `/auth/refresh` or `/auth/logout`. Target design (HttpOnly cookie + rotation) is Faza 1.2
- Roles — **8**, defined in `Domain/Identity/Roles.cs`: `SuperAdmin`, `ClinicOwner`, `ClinicAdmin`, `Dentist`, `Hygienist`, `Receptionist`, `BillingStaff`, `ReadOnly`
- Endpoint-level auth: FastEndpoints `.Roles(...)`. **Only use constants from `Roles.cs`** — `DashboardStatsEndpoint` currently passes `"Admin"` and `"OfficeManager"`, which no user can ever hold (`OfficeManager` is a *staff type* from `StaffTypes.cs`, not an auth role)
- The `tid` claim is issued at login but is **not** validated against the resolved subdomain anywhere. Do not assume client-supplied tenant is checked

## Multi-Tenancy Rules

- Resolve tenant via `IMultiTenantContextAccessor` (non-generic) — injected in handlers/services
- Never pass `tenantId` as a parameter down through the stack; always resolve from context
- `TenantAuditableEntity` sets `TenantId` automatically in `ApplicationDbContext.SaveChangesAsync`
- New module `.csproj` must be added to `DentFlow.API.csproj` and endpoint assembly scanning in `Program.cs`

## MediatR Pipeline (Order Matters)

```
Request → LoggingBehavior → ValidationBehavior (FluentValidation) → PerformanceBehavior (warns >500ms) → Handler
```

Every command/query gets validation automatically if a `IValidator<TCommand>` is registered — no manual validation calls in handlers.

## Background Jobs (Hangfire)

```csharp
// Enqueue from handler
_backgroundJobClient.Enqueue<IAppointmentReminderJob>(j => j.SendAsync(appointmentId, CancellationToken.None));

// Recurring job registration in Program.cs
RecurringJob.AddOrUpdate<IDailyReportJob>("daily-report", j => j.RunAsync(), Cron.Daily);
```

## Testing Conventions

**TDD is mandatory** — write the failing test first. See root `CLAUDE.md` and `documentation/STRATEGY.md`.

Detailed patterns (mock setup, `ValidCommand(...)` helper, assertion style) are in **`.github/copilot-instructions.md` → Unit Testing**. That guidance is good and applies here; the rules below extend it.

- Unit test handlers in isolation using `NSubstitute` mocks for repositories
- Integration tests use a real PostgreSQL test database via Testcontainers (never mock `DbContext`) — requires Docker running
- Assert on `error.Code`, never `error.Description` — the code is the contract
- Test method naming: `MethodName_Scenario_ExpectedResult`
- `.http` files in `tests/http/{module}/` for manual API exploration

### Which layer to test

Test at the lowest layer where the behaviour lives:

| Behaviour | Where |
|---|---|
| Entity invariants, status transitions, calculations | Domain unit test (`InvoiceDomainTests.cs` is the reference) |
| `ErrorOr` paths, orchestration, side effects | Handler unit test with NSubstitute |
| HTTP status codes, auth/roles, tenant isolation, query filters | Integration test |
| HTTP ↔ MediatR mapping, repository plumbing | Don't unit test — covered by integration |

This refines the `.github` rule "only test handlers": that default is right for endpoints and repositories, but **domain logic with real invariants gets its own tests** rather than being reached only through a handler.

### Running tests

```bash
dotnet test DentFlow.sln                         # everything (needs Docker for integration)
dotnet test tests/DentFlow.Patients.Tests/       # one module

# with coverage — always pass the runsettings, or generated
# migrations inflate the number past 85%
dotnet test DentFlow.sln --settings coverlet.runsettings --results-directory ./TestResults
```

Baseline at 2026-10-18: **38.3% line, 19.9% branch, 46.3% method** across 71 tests. Identity and Tenants have zero tests.
