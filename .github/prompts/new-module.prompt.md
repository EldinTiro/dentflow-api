---
mode: agent
description: Scaffold a complete new PearlDesk module following the established patterns.
---

Scaffold a complete new PearlDesk module for the entity described below. Follow the architecture and conventions in `.github/copilot-instructions.md` and `src/Modules/.instructions.md` exactly.

**Module name**: ${input:moduleName:e.g. Treatments}
**Entity name**: ${input:entityName:e.g. Treatment}
**Brief description**: ${input:description:What does this entity represent?}

---

## Files to create

Generate all files listed below. Use the existing `PearlDesk.Patients` module as the canonical reference — match naming, namespaces, access modifiers, and patterns exactly.

### Domain layer (`src/Modules/PearlDesk.${moduleName}/Domain/`)

1. **`${entityName}.cs`** — Domain entity
   - Inherits `TenantAuditableEntity`
   - Private default constructor for EF Core
   - `static ${entityName}.Create(...)` factory method
   - `void Update(...)` method that calls `SetUpdated()`
   - All properties with `private set`

2. **`${entityName}Errors.cs`** — ErrorOr error constants
   - `NotFound` — `Error.NotFound`
   - `AlreadyExists` — `Error.Conflict` (if applicable)

3. **`${entityName}Enums.cs`** — Any enums the entity uses (if needed)

### Application layer (`src/Modules/PearlDesk.${moduleName}/Application/`)

4. **`Commands/Create${entityName}Command.cs`** — `record` implementing `IRequest<ErrorOr<${entityName}Response>>`
5. **`Commands/Create${entityName}CommandHandler.cs`** — Handler with primary constructor injection
6. **`Commands/Create${entityName}CommandValidator.cs`** — FluentValidation validator
7. **`Commands/Update${entityName}Command.cs`** — includes `Guid Id`
8. **`Commands/Update${entityName}CommandHandler.cs`**
9. **`Commands/Update${entityName}CommandValidator.cs`**
10. **`Commands/Delete${entityName}Command.cs`** — `record(Guid Id)` returning `ErrorOr<Deleted>`
11. **`Commands/Delete${entityName}CommandHandler.cs`**
12. **`Queries/Get${entityName}ByIdQuery.cs`** — `record(Guid Id)` returning `ErrorOr<${entityName}Response>`
13. **`Queries/Get${entityName}ByIdQueryHandler.cs`**
14. **`Queries/List${entityName}sQuery.cs`** — with `int Page`, `int PageSize`, optional search/filter params
15. **`Queries/List${entityName}sQueryHandler.cs`**
16. **`Interfaces/I${entityName}Repository.cs`** — Repository interface with `GetByIdAsync`, `ListAsync`, `AddAsync`, `UpdateAsync`, `SoftDeleteAsync`
17. **`${entityName}Response.cs`** — `record` with `static FromEntity(${entityName} e)` factory
18. **`PagedResult.cs`** — Copy from `PearlDesk.Patients.Application` if not already present

### Endpoints (`src/Modules/PearlDesk.${moduleName}/Endpoints/`)

19. **`${entityName}CreateEndpoint.cs`** — POST `/${moduleName:lower}`, `SendCreatedAtAsync`
20. **`${entityName}GetByIdEndpoint.cs`** — GET `/${moduleName:lower}/{id}`, `SendOkAsync`
21. **`${entityName}ListEndpoint.cs`** — GET `/${moduleName:lower}`, `SendOkAsync`, inherits `EndpointWithoutRequest`
22. **`${entityName}UpdateEndpoint.cs`** — PUT `/${moduleName:lower}/{id}`, `SendOkAsync`
23. **`${entityName}DeleteEndpoint.cs`** — DELETE `/${moduleName:lower}/{id}`, `SendNoContentAsync`

All endpoints:
- Inject `ISender`
- Call `Roles(...)` with constants from `PearlDesk.Domain.Identity.Roles`
- Call `Version(1)`
- Use `SendErrorsAsync` on `result.IsError`

### Module wiring

24. **`DependencyInjection.cs`** — Module DI (typically empty — infra wires repos)
25. **`PearlDesk.${moduleName}.csproj`** — References `PearlDesk.Domain` and `PearlDesk.Application`

### Infrastructure (in `src/PearlDesk.Infrastructure/`)

26. **`Persistence/Configurations/${entityName}Configuration.cs`** — `IEntityTypeConfiguration<${entityName}>`
    - `ToTable("${entityName:snake_case}s")`
    - Enum conversions: `HasConversion<string>()`
    - Indexes always leading with `TenantId`
27. **`Persistence/Repositories/${entityName}Repository.cs`** — Implements `I${entityName}Repository`

### Steps to complete wiring (tell me after generating files)
After generating all files, remind me to:
- Add `DbSet<${entityName}>` to `ApplicationDbContext`
- Register `I${entityName}Repository` → `${entityName}Repository` in `Infrastructure/DependencyInjection.cs`
- Add module assembly to `AddApplication(...)` in `Program.cs`
- Add endpoint assembly to `o.Assemblies` in `AddFastEndpoints(...)` in `Program.cs`
- Add a test project `tests/PearlDesk.${moduleName}.Tests/`
- Run `dotnet ef migrations add Add${entityName} --project src/PearlDesk.Infrastructure`
