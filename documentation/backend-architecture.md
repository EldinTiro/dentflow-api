# DentFlow � Backend Architecture

> Backend-specific technical reference for the `server` repository.  
> Last updated: 2026-03-14

---

## Table of Contents

1. [Tech Stack](#tech-stack)
2. [Solution Structure](#solution-structure)
3. [Architecture Principles](#architecture-principles)
4. [Module Structure](#module-structure)
5. [Base Classes & Conventions](#base-classes--conventions)
6. [Authentication & Authorization](#authentication--authorization)
7. [Multi-Tenancy](#multi-tenancy)
8. [EF Core & Database](#ef-core--database)
9. [CQRS & MediatR Pipeline](#cqrs--mediatr-pipeline)
10. [Error Handling](#error-handling)
11. [Background Jobs](#background-jobs)
12. [Testing Strategy](#testing-strategy)
13. [Local Development](#local-development)
14. [Dev Workflow Script](#dev-workflow-script)

---

## Tech Stack

| Concern | Library / Tool | Version |
|---|---|---|
| Framework | ASP.NET Core | .NET 9 |
| API layer | FastEndpoints | 5.x |
| CQRS dispatching | MediatR | latest |
| Validation | FluentValidation | 12.x |
| Error handling | ErrorOr | 2.x |
| ORM | Entity Framework Core + Npgsql | 9.x |
| Multi-tenancy | Finbuckle.MultiTenant | 9.x |
| Auth | ASP.NET Core Identity + JWT RS256 | built-in |
| Background jobs | Hangfire + PostgreSQL | 1.x |
| Caching | StackExchange.Redis | 2.x |
| Logging | Serilog | 9.x |
| Testing | xUnit + NSubstitute + FluentAssertions | latest |

---

## Solution Structure

```
DentFlow-api/
+-- DentFlow.sln
+-- Dockerfile
+-- documentation/               ? This folder (BE-specific docs)
+-- src/
�   +-- DentFlow.API/           ? FastEndpoints, middleware, DI wiring, Program.cs
�   +-- DentFlow.Application/   ? MediatR handlers, pipeline behaviors, interfaces
�   +-- DentFlow.Domain/        ? Entities, value objects, enums, typed errors
�   +-- DentFlow.Infrastructure/? EF Core DbContext, repositories, external adapters
�   +-- Modules/
�       +-- DentFlow.Tenants/
�       +-- DentFlow.Identity/
�       +-- DentFlow.Staff/
�       +-- DentFlow.Patients/
�       +-- DentFlow.Appointments/
�       +-- DentFlow.Treatments/
�       +-- DentFlow.Billing/
�       +-- DentFlow.Notifications/
�       +-- DentFlow.Documents/
�       +-- DentFlow.Reporting/
+-- tests/
    +-- DentFlow.Identity.Tests/
    +-- DentFlow.Tenants.Tests/
    +-- http/                    ? .http test files per module
        +-- identity/
        +-- tenants/
        +-- ...
```

### Dependency Rules

```
API ? Infrastructure ? Application ? Domain
API ? Modules ? Application ? Domain
Tests ? Modules
```

Domain has **zero** dependencies on other layers. Infrastructure and Modules depend on Application and Domain only.

---

## Architecture Principles

- **Modular Monolith** � all modules in one deployable unit; can be extracted to microservices later
- **Clean Architecture** � dependencies always point inward toward Domain
- **CQRS** � commands mutate state, queries only read; never mixed
- **No business logic in endpoints** � endpoints map HTTP ? MediatR only
- **No cross-module DbContext access** � modules communicate via MediatR or service interfaces

---

## Module Structure

Each module under `src/Modules/DentFlow.{ModuleName}/` follows:

```
DentFlow.{ModuleName}/
+-- Domain/
�   +-- {Entity}.cs
�   +-- {Entity}Errors.cs
+-- Application/
�   +-- Commands/
�   �   +-- {Verb}{Entity}/
�   �       +-- {Verb}{Entity}Command.cs
�   �       +-- {Verb}{Entity}CommandHandler.cs
�   �       +-- {Verb}{Entity}CommandValidator.cs
�   +-- Queries/
�       +-- Get{Entity}ById/
�           +-- Get{Entity}ByIdQuery.cs
�           +-- Get{Entity}ByIdQueryHandler.cs
+-- Infrastructure/
�   +-- {Entity}Repository.cs
+-- Endpoints/
    +-- {Entity}CreateEndpoint.cs
    +-- {Entity}GetByIdEndpoint.cs
    +-- {Entity}UpdateEndpoint.cs
```

---

## Base Classes & Conventions

### `TenantAuditableEntity`
All tenant-scoped entities **must** inherit from this base class:

```csharp
public abstract class TenantAuditableEntity : BaseEntity, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void SoftDelete();      // sets IsDeleted = true, DeletedAt = now
    public void SetUpdated();      // sets UpdatedAt = now
    public void SetTenant(Guid);   // sets TenantId
}
```

**Never** create an entity without this base class. **Never** hard-delete clinical or financial records � always call `SoftDelete()`.

### Naming Conventions

| Artefact | Convention | Example |
|---|---|---|
| DB tables | `snake_case` plural | `treatment_plans` |
| C# classes | `PascalCase` | `TreatmentPlan` |
| API routes | `kebab-case` | `/api/v1/treatment-plans` |
| Commands | `{Verb}{Noun}Command` | `CreatePatientCommand` |
| Queries | `Get{Noun}By{X}Query` | `GetPatientByIdQuery` |
| Endpoints | `{Noun}{Verb}Endpoint` | `PatientCreateEndpoint` |
| Repositories | `I{Entity}Repository` | `IPatientRepository` |
| Error classes | `{Entity}Errors` | `PatientErrors` |

---

## Authentication & Authorization

### JWT (RS256)
- Signed with RSA private key, validated with RSA public key
- Access token lifetime: **15 minutes**
- Claims: `userId`, `tenantId`, `role`, `permissions[]`, `sessionId`
- `tenantId` in token cross-validated against subdomain on every request
- Keys configured in `appsettings.json` under `Jwt:PrivateKey` / `Jwt:PublicKey`

### Refresh Tokens
- Stored in **HttpOnly + Secure + SameSite=Strict** cookie
- Hashed with SHA-256 before persisting to `refresh_tokens` table
- Token family tracking � reuse of a revoked token revokes the entire family
- Configurable lifetime: `Jwt:RefreshTokenExpiryDays` (default: 7)

### Roles
`SuperAdmin` | `ClinicOwner` | `Dentist` | `Hygienist` | `Receptionist` | `BillingStaff` | `ReadOnly`

Two-layer model: coarse Roles + fine-grained Permission overrides per user.

### Identity Provider
- Default: **ASP.NET Core Identity** (no Keycloak / Auth0 / Cognito)
- Per-tenant OIDC override supported for enterprise groups (Azure AD / Okta)
  - Configured via `oidc_provider_url`, `oidc_client_id`, `oidc_client_secret` on tenant settings

### MFA
- TOTP via `OtpNet` � compatible with Google Authenticator / Authy
- Per-tenant enforcement � `ClinicOwner` can mandate MFA for all staff

---

## Multi-Tenancy

- **Finbuckle.MultiTenant** v9 with subdomain resolution: `{slug}.mydentflow.com`
- `TenantId` automatically applied to all `TenantAuditableEntity` rows on save
- **Global Query Filters** in `ApplicationDbContext` automatically scope all queries to the current tenant
- **Never** add `WHERE tenant_id = ...` manually � the filter handles it
- `tenantId` in JWT is cross-validated against the resolved subdomain on every request

---

## EF Core & Database

- **PostgreSQL 17** via Npgsql
- All timestamps: `TIMESTAMPTZ` (UTC only � never local time)
- All PKs: `UUID` / `Guid` � never `int` identity
- Money: `NUMERIC(10,2)` � never `float` or `double`
- Soft delete: `is_deleted` + `deleted_at` columns � managed by `TenantAuditableEntity`
- Migrations live in `DentFlow.Infrastructure`

### Running Migrations
```powershell
dotnet ef migrations add {MigrationName} `
    --project DentFlow-api/src/DentFlow.Infrastructure `
    --startup-project DentFlow-api/src/DentFlow.API

dotnet ef database update `
    --project DentFlow-api/src/DentFlow.Infrastructure `
    --startup-project DentFlow-api/src/DentFlow.API
```

---

## CQRS & MediatR Pipeline

Every feature follows this pattern:

```
HTTP Request
    ? FastEndpoints Endpoint         (maps request ? command/query)
    ? MediatR.Send(command/query)
        ? LoggingBehavior            (logs handler name)
        ? ValidationBehavior         (runs FluentValidation, returns 400 on failure)
        ? PerformanceBehavior        (warns if handler > 500ms)
        ? Command/QueryHandler       (business logic, returns ErrorOr<T>)
    ? Endpoint maps ErrorOr ? HTTP response
```

### Handler Pattern
```csharp
public class CreatePatientCommandHandler(IPatientRepository repo)
    : IRequestHandler<CreatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        CreatePatientCommand command, CancellationToken ct)
    {
        // Validate domain rules
        if (await repo.ExistsAsync(command.Email, ct))
            return PatientErrors.EmailAlreadyExists;

        var patient = Patient.Create(command.FirstName, command.LastName, command.Email);
        await repo.AddAsync(patient, ct);

        return new PatientResponse(patient.Id, patient.FirstName, patient.LastName);
    }
}
```

---

## Error Handling

- All handlers return `ErrorOr<T>` � **never throw** for business logic
- Typed errors defined in `Domain/` per aggregate:
  ```csharp
  public static class PatientErrors
  {
      public static readonly Error NotFound =
          Error.NotFound("Patient.NotFound", "Patient was not found.");
  }
  ```
- Endpoints map results:
  ```csharp
  return result.Match(
      patient => SendAsync(patient, 200, ct),
      errors  => SendErrorsAsync(errors, ct));
  ```
- Unhandled exceptions ? RFC 9457 `ProblemDetails` via global exception middleware

---

## Background Jobs

- **Hangfire** with PostgreSQL storage
- Dashboard restricted to `SuperAdmin` role only (`/hangfire`)
- Used for: appointment reminders, notification dispatch, report generation, token cleanup

---

## Testing Strategy

| Layer | What to test | Tools |
|---|---|---|
| MediatR handlers | Success + all error branches | xUnit + NSubstitute + FluentAssertions |
| Domain entities | Business rules, state transitions | xUnit + FluentAssertions |
| Validators | Valid and invalid input cases | xUnit + FluentValidation.TestHelper |
| Endpoints | **Not tested directly** � covered by `.http` files | � |

One test project per module: `tests/DentFlow.{ModuleName}.Tests/`

```csharp
// Example handler test pattern
[Fact]
public async Task Handle_WhenPatientNotFound_ReturnsNotFoundError()
{
    // Arrange
    _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
         .Returns((Patient?)null);

    // Act
    var result = await _handler.Handle(new GetPatientByIdQuery(Guid.NewGuid()), default);

    // Assert
    result.IsError.Should().BeTrue();
    result.FirstError.Should().Be(PatientErrors.NotFound);
}
```

---

## Local Development

### Prerequisites
- .NET 9 SDK
- Docker Desktop
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### Start Infrastructure
```powershell
docker compose up -d
```
Starts PostgreSQL 17 (port 5432) and Redis 7 (port 6379).

### Run API
```powershell
dotnet run --project DentFlow-api/src/DentFlow.API
```
API available at `http://localhost:5000`  
Swagger at `http://localhost:5000/swagger`  
Health check at `http://localhost:5000/health`

### Environment
Copy `appsettings.Development.json` and set:
- `Jwt:PrivateKey` � RSA private key (PEM format)
- `Jwt:PublicKey` � RSA public key (PEM format)

---

## Dev Workflow Script

The `dev-workflow.ps1` script at the **monorepo root** runs the full CI loop locally:

```powershell
# Full workflow with migration
.\dev-workflow.ps1 -Module Identity -Migration AddRefreshTokensTable

# Without migration
.\dev-workflow.ps1 -Module Patients
```

| Step | Action |
|---|---|
| 1 | `docker compose up -d` + health check |
| 2 | EF Core migration (if `-Migration` supplied) |
| 3 | `dotnet build` � must pass with 0 errors |
| 4 | `dotnet test` for the module |
| 5 | API start + `/health` check |
| 6 | `.http` file execution for the module |

A feature is **only complete** when all 6 steps pass.

