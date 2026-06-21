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
- **Naming**: snake_case columns (configured in `ApplicationDbContext` via `UseSnakeCaseNamingConvention()`)
- **Tenant scoping**: Automatic via EF global query filters — never add manual `WHERE tenant_id = ...`
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

- JWT Bearer, 15-min access token, HttpOnly cookie refresh token
- Roles: `SuperAdmin`, `ClinicOwner`, `Dentist`, `Hygienist`, `Receptionist`, `BillingStaff`, `ReadOnly`
- Endpoint-level auth: `[Authorize(Roles = "Dentist,ClinicOwner")]` or FastEndpoints `.Roles(...)` 
- `tenantId` claim is validated against resolved subdomain on every request — never trust client-supplied tenant

## Multi-Tenancy Rules

- Resolve tenant via `IMultiTenantContextAccessor<TenantInfo>` — injected in handlers/services
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

- Unit test handlers in isolation using `NSubstitute` mocks for repositories
- Integration tests use a real PostgreSQL test database (never mock DbContext)
- `.http` files in `tests/http/{module}/` for manual API exploration
- Test method naming: `MethodName_Scenario_ExpectedResult`
