# DentFlow API — Copilot Instructions

This is the **DentFlow API**: a multi-tenant SaaS dental practice management backend.
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
| Multi-tenancy | Finbuckle.MultiTenant — subdomain strategy (`{slug}.DentFlow.com`) |
| Logging | Serilog |
| Caching | Redis (optional, configured via `Redis:ConnectionString`) |
| Testing | xUnit + FluentAssertions + NSubstitute |

---

## Solution Structure

```
src/
  DentFlow.API/            # Entry point: FastEndpoints, middleware, DI composition
  DentFlow.Application/    # MediatR pipeline behaviors, shared application contracts
  DentFlow.Domain/         # Entities, enums, errors — no external dependencies
  DentFlow.Infrastructure/ # EF Core DbContext, repositories, migrations
  Modules/
    DentFlow.Identity/     # Registration, login, JWT issuance
    DentFlow.Patients/     # Patient records
    DentFlow.Staff/        # Staff members, availability
    DentFlow.Appointments/ # Appointment scheduling and status lifecycle
    DentFlow.Tenants/      # Tenant management
    DentFlow.Billing/      # (planned)
    DentFlow.Treatments/   # (planned)
    DentFlow.Notifications/# (planned)
    DentFlow.Documents/    # (planned)
    DentFlow.Reporting/    # (planned)
tests/
  DentFlow.<Module>.Tests/ # Unit tests per module (xUnit)
```

**Dependency direction**: `API → Infrastructure → Application → Domain`
Modules depend on `Application` and `Domain` only. Modules never reference each other.

---

## Module Internal Structure

Every module follows this exact folder layout:

```
DentFlow.<Module>/
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
  DentFlow.<Module>.csproj
```

---

## Domain Model Rules

1. All tenant-scoped entities **must** inherit `TenantAuditableEntity` (from `DentFlow.Domain.Common`).
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
            foreach (var error in result.Errors)
                AddError(error.Description);   // ← Description only, never Code
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
- **Always call `AddError(error.Description)` for each error before `SendErrorsAsync`** — never pass just `error.Code` as the first argument. FastEndpoints treats dotted strings as property names, discards them, and swaps the arguments if the property doesn't match a request field. Passing only the description avoids this entirely.
- Never call `SendErrorsAsync` without a preceding `AddError` — doing so sends `errors: []`, which gives the frontend nothing to display.
- New endpoints must be registered in `Program.cs` under `o.Assemblies`.

---

## Error Handling

### Defining Errors (Domain layer)

```csharp
// Domain/<Module>/<Entity>Errors.cs
public static class PatientErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Patient.NotFound", "Patient was not found.");

    public static readonly Error AlreadyExists =
        Error.Conflict("Patient.AlreadyExists", "A patient with this email already exists.");
}
```

Rules for error descriptions:
- Write them as **user-readable sentences** — they are sent directly to the frontend.
- Include actionable guidance where helpful (e.g. "Please choose a different date or provider.").
- Never use technical identifiers or internal codes in the description string.

### Propagating Errors in Endpoints

```csharp
// ✅ Correct — description reaches the frontend
if (result.IsError)
{
    foreach (var error in result.Errors)
        AddError(error.Description);
    await SendErrorsAsync(cancellation: ct);
    return;
}

// ❌ Wrong — AddError(code, description) puts the code into the `reason` field
foreach (var error in result.Errors)
    AddError(error.Code, error.Description);

// ❌ Wrong — sends errors: [] with no messages
if (result.IsError) { await SendErrorsAsync(cancellation: ct); return; }
```

### FastEndpoints Wire Format

The JSON response for a 400/409/404 always has this shape:
```json
{
  "type": "...",
  "title": "Bad Request",
  "status": 400,
  "errors": [
    { "name": "generalErrors", "reason": "Patient was not found." }
  ]
}
```
`name` is always `"generalErrors"` for non-field errors. `reason` carries the human-readable description.

### Handler Rules
- All handler return types are `ErrorOr<T>`, `ErrorOr<Deleted>`, or `ErrorOr<Updated>`.
- Never throw exceptions for expected business errors — always return an `Error`.
- `ValidationBehavior` runs FluentValidation before the handler and returns validation errors automatically.

---

## Repository Pattern

**Interface** lives in the module: `Application/Interfaces/I<Entity>Repository.cs`
**Implementation** lives in: `DentFlow.Infrastructure/Persistence/Repositories/<Entity>Repository.cs`
**Registration** in: `DentFlow.Infrastructure/DependencyInjection.cs`

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
- The tenant is resolved from the subdomain (`{slug}.DentFlow.com`) by Finbuckle before any request hits a handler.

---

## Authorization & Roles

Roles are defined in `DentFlow.Domain.Identity.Roles`:
- `SuperAdmin` — global platform admin
- `ClinicOwner` — tenant owner, full access within tenant
- `ClinicAdmin` — clinic admin
- `Dentist` / `Hygienist` — clinical providers
- `Receptionist` — front desk
- `BillingStaff` — billing only
- `ReadOnly` — read-only access

Always specify roles on endpoints using `Roles(...)` in `Configure()`. Always import `Roles` from `DentFlow.Domain.Identity`.

---

## MediatR Pipeline Behaviors (execution order)

1. `LoggingBehavior` — logs request name and duration
2. `ValidationBehavior` — runs FluentValidation; short-circuits with `ErrorOr` validation errors
3. `PerformanceBehavior` — logs a warning if request takes > 500 ms

These are registered in `DentFlow.Application/DependencyInjection.cs` and apply to all handlers automatically.

---

## API Conventions

- Route prefix: `api/` (configured in `Program.cs`)
- Versioning prefix: `v` (e.g. `api/v1/patients`)
- All routes are lowercase plural nouns: `/patients`, `/staff`, `/appointments`
- Rate limit: 300 requests/minute per tenant (applied globally)

---

## Unit Testing

### Stack
| Tool | Role |
|---|---|
| xUnit | Test runner |
| FluentAssertions | Assertion DSL (`result.IsError.Should().BeTrue()`) |
| NSubstitute | Mocking (`Substitute.For<IInterface>()`) |

### Structure & Conventions

```csharp
public class BookAppointmentCommandHandlerTests
{
    // Mock dependencies — one field per injected interface
    private readonly IAppointmentRepository _repo = Substitute.For<IAppointmentRepository>();
    private readonly IAppointmentTypeRepository _typeRepo = Substitute.For<IAppointmentTypeRepository>();
    private readonly BookAppointmentCommandHandler _sut;

    public BookAppointmentCommandHandlerTests()
    {
        _sut = new BookAppointmentCommandHandler(_repo, _typeRepo);
    }

    // Static helper with sensible defaults — tests only override what they're testing
    private static BookAppointmentCommand ValidCommand(DateTime? startAt = null, DateTime? endAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddHours(1);
        var end   = endAt   ?? DateTime.UtcNow.AddHours(2);
        return new BookAppointmentCommand(
            PatientId: Guid.NewGuid(),
            ProviderId: Guid.NewGuid(),
            AppointmentTypeId: Guid.NewGuid(),
            StartAt: start, EndAt: end,
            ChiefComplaint: "Checkup", Notes: null,
            OperatoryId: null, IsNewPatient: false, Source: "Staff");
    }

    [Fact]
    public async Task Handle_ValidCommand_BooksAppointment()
    {
        // Arrange
        var command = ValidCommand();
        _typeRepo.GetByIdAsync(command.AppointmentTypeId, Arg.Any<CancellationToken>())
                 .Returns(AppointmentType.Create("Checkup", 60));
        _repo.HasProviderConflictAsync(
            command.ProviderId, command.StartAt, command.EndAt,
            null, Arg.Any<CancellationToken>()).Returns(false);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be(AppointmentStatus.Scheduled);
        await _repo.Received(1).AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProviderConflict_ReturnsConflictError()
    {
        // Arrange
        var command = ValidCommand();
        _typeRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
                 .Returns(AppointmentType.Create("Checkup", 60));
        _repo.HasProviderConflictAsync(
            Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
            Arg.Any<Guid?>(), Arg.Any<CancellationToken>()).Returns(true);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Appointment.ProviderConflict");
        await _repo.DidNotReceive().AddAsync(Arg.Any<Appointment>(), Arg.Any<CancellationToken>());
    }
}
```

### Rules
- **Only test handlers** — not endpoints, not repositories, not domain methods in isolation unless the logic is complex enough to warrant it.
- Test method name format: `MethodName_Scenario_ExpectedBehavior` (e.g. `Handle_ProviderConflict_ReturnsConflictError`).
- Create a `ValidCommand(...)` static helper in each handler test class so each test overrides only the field it cares about.
- Assert on `result.FirstError.Code` — the error `Code` is the contract. **Do not assert on `error.Description`** — it is a display string that can change.
- Use `Received(n)` / `DidNotReceive()` to verify side effects (e.g. that `AddAsync` was or was not called).
- Always pass `CancellationToken.None` in tests.
- One concern per `[Fact]` — if you're testing three error conditions, write three separate tests.

---

## DateTime Conventions

- Always use `DateTime.UtcNow` — **never** `DateTime.Now`. All timestamps are stored and handled in UTC.
- Use `DateTime` (not `DateTimeOffset`) throughout domain, application, and infrastructure unless cross-timezone offset tracking is explicitly required.
- All `DateTime` properties in domain entities, commands, queries, and repositories are assumed to be UTC.
- The frontend sends ISO 8601 strings (e.g. `"2026-03-17T09:00:00Z"`). ASP.NET Core / JSON deserializer converts them to UTC `DateTime` automatically when the `Z` suffix is present.
- Never store local time in the database.
- `HasConversion<string>()` is reserved for enums. Datetimes are stored as `timestamptz` (PostgreSQL UTC timestamp) — do not add string conversion for them.

---

## FluentValidation Patterns

Validators are auto-discovered and run through the `ValidationBehavior` pipeline. The handler never receives an invalid command.

```csharp
public class BookAppointmentCommandValidator : AbstractValidator<BookAppointmentCommand>
{
    public BookAppointmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.StartAt).NotEmpty();
        RuleFor(x => x.EndAt)
            .NotEmpty()
            .GreaterThan(x => x.StartAt)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.ChiefComplaint)
            .MaximumLength(500)
            .When(x => x.ChiefComplaint is not null);
    }
}
```

Rules:
- Validate **format and presence** only in validators. Validate **business rules** (e.g. provider conflicts, tenant limits) in handlers.
- Use `.WithMessage("...")` on any rule whose default message is unclear.
- Optional fields that can be null: guard with `.When(x => x.Field is not null)`.
- Use `.NotEmpty()` for GUIDs (catches `Guid.Empty`) — not `.NotNull()`.

---

## Migrations

Run from the solution root or the `dentflow-api` directory:

```bash
# Add a migration
dotnet ef migrations add <MigrationName> \
  --project src/DentFlow.Infrastructure \
  --startup-project src/DentFlow.API

# Apply to the database (dev)
dotnet ef database update \
  --project src/DentFlow.Infrastructure \
  --startup-project src/DentFlow.API
```

Rules:
- Migration files live in `src/DentFlow.Infrastructure/Migrations/`.
- Always review the generated migration file before applying — confirm no unexpected `DROP` or `ALTER` operations.
- Add EF entity configuration in `src/DentFlow.Infrastructure/Persistence/Configurations/` (one file per entity, implements `IEntityTypeConfiguration<T>`).
- Register new `DbSet<Entity>` in `ApplicationDbContext` before adding the migration.
- New modules must also add their entity configuration and `DbSet` — see "Wiring New Modules" in `src/Modules/.instructions.md`.

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

---

## Planned Modules & Roadmap

The full product vision is documented in `documentation/implementation-plan.md`. Key context for agents:

### Module Status

| Module | Status | Notes |
|---|---|---|
| `DentFlow.Identity` | ✅ Implemented | Login, register, JWT, roles |
| `DentFlow.Tenants` | ✅ Implemented | Tenant management |
| `DentFlow.Staff` | ✅ Implemented | Staff profiles, availability |
| `DentFlow.Patients` | ✅ Implemented | Full CRUD, medical history, allergies |
| `DentFlow.Appointments` | ✅ Implemented | Booking, status lifecycle |
| `DentFlow.Treatments` | 🔲 Planned (Phase 3) | Treatment plans, CDT codes, dental chart, SOAP notes |
| `DentFlow.Billing` | 🔲 Planned (Phase 4) | Invoicing, Stripe payments, insurance claims |
| `DentFlow.Notifications` | 🔲 Planned (Phase 2/4) | Email (SES/SendGrid) + SMS (Twilio), Hangfire reminders |
| `DentFlow.Documents` | 🔲 Planned (Phase 3) | S3 file uploads, signed URLs, per-patient docs |
| `DentFlow.Reporting` | 🔲 Planned (Phase 4) | Revenue, utilisation, PDF/Excel export |

### Roles — Current vs Planned

Roles currently implemented in `DentFlow.Domain.Identity.Roles`:
- `SuperAdmin`, `ClinicOwner`, `ClinicAdmin`, `Dentist`, `Hygienist`, `Receptionist`, `BillingStaff`, `ReadOnly`

Roles **planned but not yet added** (do not use until explicitly implemented):
- `DentalAssistant` — clinical support
- `Patient` — patient portal access

### Background Jobs
- **Hangfire** with PostgreSQL storage is the planned background job processor.
- Use Hangfire for: appointment reminder scheduling, notification delivery, tenant seeding jobs.
- Hangfire dashboard will be served by the API at `/hangfire`, restricted to `SuperAdmin`.

### External Service Integrations (Planned)
| Service | Provider | Module |
|---|---|---|
| Email | AWS SES / SendGrid | Notifications |
| SMS | Twilio | Notifications |
| Payments | Stripe.NET | Billing |
| File storage | AWS S3 | Documents |
| DNS / subdomains | AWS Route 53 | Infrastructure |

### Deployment Target
- **AWS ECS Fargate** (containerised via Docker)
- **RDS PostgreSQL 17** for the database
- **ElastiCache Redis** for caching
- **Terraform** for infrastructure as code
- **GitHub Actions** for CI/CD (build → test → push image)

### Implementation Phases Summary
- **Phase 1** ✅ Foundation — solution skeleton, EF, Identity, multi-tenancy, CI/CD
- **Phase 2** ✅ Core backend — Staff, Patients, Appointments, basic notifications
- **Phase 3** 🔲 Clinical — Treatments, dental chart, SOAP notes, Documents
- **Phase 4** 🔲 Business — Billing, Stripe, Reporting, SMS reminders
- **Phase 5** 🔲 Production — Terraform/AWS, security audit, Playwright E2E, monitoring
