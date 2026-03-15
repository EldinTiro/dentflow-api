# PearlDesk API — Copilot Instructions

This is the **PearlDesk API**: a multi-tenant SaaS dental practice management backend.
Always apply every rule in this file. Never deviate without an explicit user instruction.

---

## Tech Stack

| Layer | Technology |
|---|---|
| HTTP / Routing | FastEndpoints (not Minimal APIs, not MVC controllers) |
| CQRS dispatch | MediatR |
| Validation | FluentValidation (auto-registered, runs via pipeline behavior) |
| Error handling | ErrorOr (`ErrorOr<T>` on all handler return types) |
| ORM | EF Core 8 + Npgsql (PostgreSQL) |
| Auth | ASP.NET Core Identity + JWT Bearer (HMAC-SHA256 dev, RS256 prod) |
| Multi-tenancy | Finbuckle.MultiTenant — subdomain strategy (`{slug}.pearldesk.com`) |
| Logging | Serilog |
| Caching | Redis (optional, configured via `Redis:ConnectionString`) |
| Testing | xUnit + FluentAssertions + NSubstitute |

---

## Solution Structure

```
src/
  PearlDesk.API/            # Entry point: FastEndpoints, middleware, DI composition
  PearlDesk.Application/    # MediatR pipeline behaviors, shared application contracts
  PearlDesk.Domain/         # Entities, enums, errors — no external dependencies
  PearlDesk.Infrastructure/ # EF Core DbContext, repositories, migrations
  Modules/
    PearlDesk.Identity/     # Registration, login, JWT issuance
    PearlDesk.Patients/     # Patient records
    PearlDesk.Staff/        # Staff members, availability
    PearlDesk.Appointments/ # Appointment scheduling and status lifecycle
    PearlDesk.Tenants/      # Tenant management
    PearlDesk.Billing/      # (planned)
    PearlDesk.Treatments/   # (planned)
    PearlDesk.Notifications/# (planned)
    PearlDesk.Documents/    # (planned)
    PearlDesk.Reporting/    # (planned)
tests/
  PearlDesk.<Module>.Tests/ # Unit tests per module (xUnit)
```

**Dependency direction**: `API → Infrastructure → Application → Domain`
Modules depend on `Application` and `Domain` only. Modules never reference each other.

---

## Module Internal Structure

Every module follows this exact folder layout:

```
PearlDesk.<Module>/
  Domain/
    <Entity>.cs               # Domain model — inherits TenantAuditableEntity
    <Entity>Errors.cs         # Static ErrorOr error definitions
    <Entity>Enums.cs          # Enums used by this entity
  Application/
    Commands/
      Create<Entity>Command.cs
      Create<Entity>CommandHandler.cs
      Create<Entity>CommandValidator.cs
      Update<Entity>Command.cs
      Update<Entity>CommandHandler.cs
      Update<Entity>CommandValidator.cs
      Delete<Entity>Command.cs
      Delete<Entity>CommandHandler.cs
    Queries/
      Get<Entity>ByIdQuery.cs
      Get<Entity>ByIdQueryHandler.cs
      List<Entity>sQuery.cs
      List<Entity>sQueryHandler.cs
    Interfaces/
      I<Entity>Repository.cs  # Repository contract (consumed by handlers)
    <Entity>Response.cs       # Response DTO with static FromEntity() factory
    PagedResult.cs            # Only if module needs pagination
  Endpoints/
    <Entity>CreateEndpoint.cs
    <Entity>GetByIdEndpoint.cs
    <Entity>ListEndpoint.cs
    <Entity>UpdateEndpoint.cs
    <Entity>DeleteEndpoint.cs
  DependencyInjection.cs      # Module-level DI (usually empty — infra wires repos)
  PearlDesk.<Module>.csproj
```

---

## Domain Model Rules

1. All tenant-scoped entities **must** inherit `TenantAuditableEntity` (from `PearlDesk.Domain.Common`).
2. All properties have **`private set`** — they are never assigned from outside the class.
3. Use a **static factory method** `Entity.Create(...)` instead of a public constructor.
4. Any domain method that mutates state **must call `SetUpdated()`** at the end.
5. **Never hard-delete** tenant data. Always call `SoftDelete()` from `TenantAuditableEntity`.
6. Enums are stored as `string` in the database (configured in EF via `HasConversion<string>()`).
7. Error definitions live in a static class `<Entity>Errors` using `Error.NotFound`, `Error.Conflict`, `Error.Validation`, etc.
8. The `Domain` project has **zero external NuGet dependencies**.

```csharp
// ✅ Correct domain model pattern
public class Patient : TenantAuditableEntity
{
    public string FirstName { get; private set; } = null!;
    public string LastName  { get; private set; } = null!;

    private Patient() { }  // EF Core constructor

    public static Patient Create(string patientNumber, string firstName, string lastName, ...)
    {
        var patient = new Patient();
        patient.PatientNumber = patientNumber;
        patient.FirstName = firstName;
        // ...
        return patient;
    }

    public void Update(string firstName, string lastName, ...)
    {
        FirstName = firstName;
        LastName  = lastName;
        SetUpdated();   // ← always call this
    }
}
```

---

## CQRS Pattern

### Commands
```csharp
// Command — record, immutable
public record CreatePatientCommand(
    string FirstName,
    string LastName,
    string? Email
    // ...
) : IRequest<ErrorOr<PatientResponse>>;

// Handler — primary constructor injection
public class CreatePatientCommandHandler(IPatientRepository patientRepository)
    : IRequestHandler<CreatePatientCommand, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        CreatePatientCommand command, CancellationToken ct)
    {
        // 1. Guard / duplicate check
        // 2. Create entity via factory method
        // 3. Persist via repository
        // 4. Return DTO via Response.FromEntity()
    }
}

// Validator — auto-registered, runs through ValidationBehavior pipeline
public class CreatePatientCommandValidator : AbstractValidator<CreatePatientCommand>
{
    public CreatePatientCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        // ...
    }
}
```

### Queries
```csharp
public record GetPatientByIdQuery(Guid Id) : IRequest<ErrorOr<PatientResponse>>;

public class GetPatientByIdQueryHandler(IPatientRepository patientRepository)
    : IRequestHandler<GetPatientByIdQuery, ErrorOr<PatientResponse>>
{
    public async Task<ErrorOr<PatientResponse>> Handle(
        GetPatientByIdQuery query, CancellationToken ct)
    {
        var patient = await patientRepository.GetByIdAsync(query.Id, ct);
        if (patient is null) return PatientErrors.NotFound;
        return PatientResponse.FromEntity(patient);
    }
}
```

---

## Response DTO Pattern

```csharp
public record PatientResponse(
    Guid Id,
    string PatientNumber,
    string FirstName,
    string LastName
    // ...
)
{
    // Static factory — the only way to create a response
    public static PatientResponse FromEntity(Patient p) =>
        new(p.Id, p.PatientNumber, p.FirstName, p.LastName, ...);
}
```

Never use AutoMapper. Always use a static `FromEntity()` method on the response record.

---

## FastEndpoints Pattern

```csharp
public class PatientCreateEndpoint(ISender sender)
    : Endpoint<CreatePatientRequest, PatientResponse>
{
    public override void Configure()
    {
        Post("/patients");
        Roles(
            Roles.ClinicOwner, Roles.ClinicAdmin,
            Roles.Receptionist, Roles.Dentist,
            Roles.Hygienist, Roles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "Create a new patient");
    }

    public override async Task HandleAsync(CreatePatientRequest req, CancellationToken ct)
    {
        var command = new CreatePatientCommand(req.FirstName, req.LastName, req.Email, ...);
        var result  = await sender.Send(command, ct);

        if (result.IsError)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        await SendCreatedAtAsync<PatientGetByIdEndpoint>(
            new { id = result.Value.Id },
            result.Value,
            cancellation: ct);
    }
}

// Separate request DTO — mirrors the command but is its own type
public record CreatePatientRequest(
    string FirstName,
    string LastName,
    string? Email
    // ...
);
```

**Rules:**
- Inject `ISender` (not `IMediator`) for MediatR dispatch.
- Always use `Roles(...)` — never use `[Authorize]` attributes.
- Map `req` → command manually; never pass `req` directly to MediatR.
- Use `SendCreatedAtAsync` for POST, `SendOkAsync` for GET/PUT, `SendNoContentAsync` for DELETE.
- Use `SendErrorsAsync` when `result.IsError` — FastEndpoints maps `ErrorOr` errors to problem details automatically.
- New endpoints must be registered in `Program.cs` under `o.Assemblies`.

---

## Error Handling

```csharp
// Definition (in Domain/<Module>/<Entity>Errors.cs)
public static class PatientErrors
{
    public static readonly Error NotFound    = Error.NotFound("Patient.NotFound", "Patient was not found.");
    public static readonly Error AlreadyExists = Error.Conflict("Patient.AlreadyExists", "A patient with this email already exists.");
}

// Usage in handler
if (existing is not null) return PatientErrors.AlreadyExists;
```

- All handler return types are `ErrorOr<T>` or `ErrorOr<Deleted>` / `ErrorOr<Updated>`.
- Never throw exceptions for expected business errors.
- `ValidationBehavior` automatically returns `ErrorOr` validation errors before handlers run.

---

## Repository Pattern

**Interface** lives in the module: `Application/Interfaces/I<Entity>Repository.cs`
**Implementation** lives in: `PearlDesk.Infrastructure/Persistence/Repositories/<Entity>Repository.cs`
**Registration** in: `PearlDesk.Infrastructure/DependencyInjection.cs`

```csharp
// Interface (module)
public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Patient?> GetByEmailAsync(string? email, CancellationToken ct = default);
    Task<(IReadOnlyList<Patient> Items, int Total)> ListAsync(
        string? searchTerm, PatientStatus? status, int page, int pageSize, CancellationToken ct = default);
    Task<string> GeneratePatientNumberAsync(CancellationToken ct = default);
    Task AddAsync(Patient patient, CancellationToken ct = default);
    Task UpdateAsync(Patient patient, CancellationToken ct = default);
    Task SoftDeleteAsync(Patient patient, CancellationToken ct = default);
}

// Implementation (Infrastructure) — primary constructor
public class PatientRepository(ApplicationDbContext dbContext) : IPatientRepository
{
    // Use dbContext.Patients (EF Set) — global query filters apply automatically
    // Call SaveChangesAsync after every write
}
```

---

## Multi-Tenancy Rules

- **Never** manually filter queries by `TenantId`. The EF Core global query filter on `ApplicationDbContext` handles tenant isolation automatically for all `TenantAuditableEntity` types.
- **Never** manually filter out soft-deleted records. The global filter handles `IsDeleted == false` automatically.
- When creating a new entity that requires a tenant, call `entity.SetTenant(tenantId)` where `tenantId` comes from `IMultiTenantContextAccessor`.
- The tenant is resolved from the subdomain (`{slug}.pearldesk.com`) by Finbuckle before any request hits a handler.

---

## Authorization & Roles

Roles are defined in `PearlDesk.Domain.Identity.Roles`:
- `SuperAdmin` — global platform admin
- `ClinicOwner` — tenant owner, full access within tenant
- `ClinicAdmin` — clinic admin
- `Dentist` / `Hygienist` — clinical providers
- `Receptionist` — front desk
- `BillingStaff` — billing only
- `ReadOnly` — read-only access

Always specify roles on endpoints using `Roles(...)` in `Configure()`. Always import `Roles` from `PearlDesk.Domain.Identity`.

---

## MediatR Pipeline Behaviors (execution order)

1. `LoggingBehavior` — logs request name and duration
2. `ValidationBehavior` — runs FluentValidation; short-circuits with `ErrorOr` validation errors
3. `PerformanceBehavior` — logs a warning if request takes > 500 ms

These are registered in `PearlDesk.Application/DependencyInjection.cs` and apply to all handlers automatically.

---

## API Conventions

- Route prefix: `api/` (configured in `Program.cs`)
- Versioning prefix: `v` (e.g. `api/v1/patients`)
- All routes are lowercase plural nouns: `/patients`, `/staff`, `/appointments`
- Rate limit: 300 requests/minute per tenant (applied globally)
- Health check: `GET /health` (anonymous)
- Errors use RFC 7807 problem details (`c.Errors.UseProblemDetails()`)

---

## What NOT to Do

- Do not use MVC controllers — use FastEndpoints only.
- Do not use `[ApiController]` or `[Route]` attributes.
- Do not use AutoMapper.
- Do not use public constructors on domain entities — use factory methods.
- Do not assign to entity properties from outside the entity — all setters are private.
- Do not add `TenantId` filters to queries — EF global filters handle this.
- Do not hard-delete tenant data — always call `SoftDelete()`.
- Do not throw exceptions for expected business errors — return `ErrorOr` errors.
- Do not put business logic in endpoints — endpoints only translate HTTP ↔ MediatR.
- Do not reference one module from another module — go through the Application/Domain layers.
