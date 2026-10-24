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
- **Tenant scoping**: Automatic via EF global query filters — never add manual `WHERE tenant_id = ...`, and never `IgnoreQueryFilters()` + a hand-written tenant check (that is how the dashboard read zeros for every clinic, QA-003). Tenant resolves from the `tid` claim, then the subdomain; `TenantStore` reads the `tenants` table
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
        var result = await sender.Send(req.ToCommand(), ct);
        // Maps ErrorType → status (404/409/400…) and sends every error description.
        // A bare SendErrorsAsync() here is rejected by DentFlow.Architecture.Tests (QA-005).
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
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
- **Refresh tokens**: `/auth/refresh` rotates an HttpOnly cookie (path `/api/v1/auth`) and revokes the whole family on reuse; `/auth/logout` revokes it. Only a hash is stored
- **Suspended clinics**: a deactivated tenant cannot log in (403 `Tenant.Inactive`) or refresh, and `SuspendedTenantMiddleware` answers 403 to its existing access tokens
- **Temporary passwords**: users created by invite/provisioning or reset by an admin get `MustChangePassword`; their token carries `pwd_change` and `PasswordChangeRequiredMiddleware` blocks everything but `/auth/*` and `/users/me*` until they change it
- **User-management guards** live in the Identity handlers (`UserManagementGuard`): no self role-change/deactivation, a clinic keeps an active owner, the platform keeps an active SuperAdmin, only owners touch owners
- Roles — **5**, defined in `Domain/Identity/Roles.cs`: `SuperAdmin`, `ClinicOwner`, `Dentist`, `DentalNurse`, `Receptionist` (`Roles.TenantRoles` = the four clinic roles). Retired roles (`ClinicAdmin`, `Hygienist`, `BillingStaff`, `ReadOnly`) are mapped or deactivated at startup by `SuperAdminSeeder.SyncRolesAsync`
- **Permissions** (`Domain/Identity/Permission.cs`) and who holds them (`RolePermissions`) are the one place that says who may do what. Write endpoints use `Policies(Permission.X)`, not role strings: `people.manage` (owner — staff and their sign-in), `patients.manage` (every clinic role, not SuperAdmin), `billing.view` / `billing.collect`, `finance.view` / `finance.adjust` (owner), `dashboard.view`. Reads open to every clinic role use `Roles([.. Roles.TenantRoles])`. `RoleNameRules` (architecture tests) rejects unknown role strings
- Endpoint-level auth on endpoints that are not converted yet still uses FastEndpoints `.Roles(...)` with role names — **only use constants from `Roles.cs`** (`OfficeManager` is a *staff type*, not an auth role)
- The `tid` claim is issued at login but is **not** validated against the resolved subdomain anywhere. Do not assume client-supplied tenant is checked

### People: one staff profile, optional sign-in (people-merge plan)

- A person is a `StaffMember`. Signing in is an **optional** `ApplicationUser` linked by `StaffMember.UserId` (unique per clinic). There is no separate Team list
- `POST /staff { …, access?: { role } }` creates profile + account in one transaction and returns `tempPassword` once; `POST /staff/{id}/access { role }` gives access later. The linked email is the login email and can't be edited on the profile (`Staff.EmailLockedByAccess`); names stay in sync both ways
- Staff responses carry `access` (`StaffAccessSummary`: role, active, must-change-password, last sign-in) and `isBookable` (active + clinical staff type) — clients use `isBookable`, never their own staff-type lists
- Clinic-side account actions: `PUT /users/{id}/role`, `PUT /users/{id}/status`, `POST /users/{id}/reset-password` (all `people.manage`, guarded by `UserManagementGuard`)
- Cross-module seams in `DentFlow.Application`: `ITransactionRunner` (one transaction across modules; joins an open one), `IUserAccountDirectory` (access summaries), `IUserAccountLifecycle` (deactivate / rename an account with the guards), `IStaffDirectory`, `IAppointmentReader`, `IPatientHistoryReader`, `IPatientDirectory`
- `StaffAccountBackfill` runs at startup after the role sync: every clinic account gets exactly one profile (link by email, else create)

### Deactivate instead of delete

- Patients: `POST /patients/{id}/deactivate { reason? }` / `reactivate` (any clinic role). Refused while appointments are ahead: 409 `Patient.HasUpcomingAppointments` with the count as error metadata (`"Patient.HasUpcomingAppointments.count"`). Inactive patients are left out of `GET /patients` (default `status=Active`; `status=all` lifts it), search, and booking (409 `Patient.Inactive`). `DELETE /patients/{id}` only for a duplicate without history, else 409 `Patient.HasHistory`
- Staff: `POST /staff/{id}/deactivate` / `reactivate` (owner). Deactivation also deactivates the linked account in the same transaction (self / last owner refused); reactivation leaves the account off. `GET /staff` defaults to active (`isActive=all` for everyone — use it wherever past appointments need provider names). `DELETE /staff/{id}` only without appointments, else 409 `Staff.HasHistory`
- Allergies and emergency contacts are soft-deleted
- `SendResultErrorsAsync` sends error metadata as extra `{ name: "code.key", reason: value }` entries

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
