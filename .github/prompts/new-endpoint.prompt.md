---
mode: agent
description: Add a new FastEndpoint to an existing PearlDesk module.
---

Add a new FastEndpoint to the specified module. Follow all conventions in `.github/copilot-instructions.md` and `src/Modules/.instructions.md`. Use existing endpoints in `PearlDesk.Patients/Endpoints/` as the canonical reference.

**Module**: ${input:moduleName:e.g. Patients}
**Entity**: ${input:entityName:e.g. Patient}
**HTTP method**: ${input:httpMethod:GET | POST | PUT | DELETE}
**Route**: ${input:route:e.g. /patients/{id}/archive}
**MediatR message to dispatch**: ${input:mediatRMessage:e.g. ArchivePatientCommand}
**Roles allowed** (comma-separated): ${input:roles:e.g. ClinicOwner, ClinicAdmin, Dentist}
**Summary / description**: ${input:summary:One-line description for Swagger}

---

## Files to create

### 1. Endpoint (`Endpoints/${entityName}${input:action}Endpoint.cs`)

```csharp
public class ${entityName}${input:action}Endpoint(ISender sender)
    : Endpoint<${input:action}${entityName}Request, ${entityName}Response>  // adjust TResponse as needed
{
    public override void Configure()
    {
        ${httpMethod}("${route}");
        Roles(${roles — use Roles.ConstantName, not string literals});
        Version(1);
        Summary(s => s.Summary = "${summary}");
    }

    public override async Task HandleAsync(${input:action}${entityName}Request req, CancellationToken ct)
    {
        var command = new ${mediatRMessage}(/* map from req */);
        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        // Choose the appropriate send method:
        // POST creating a resource   → await SendCreatedAtAsync<${entityName}GetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
        // GET / PUT                  → await SendOkAsync(result.Value, cancellation: ct);
        // DELETE / action no body    → await SendNoContentAsync(cancellation: ct);
    }
}
```

### 2. Request DTO (in the same file or its own file)

```csharp
public record ${input:action}${entityName}Request(
    // Route-bound: [FromRoute] params are matched by name automatically in FastEndpoints
    Guid Id
    // Body params as needed
);
```

**Rules enforced:**
- Inject `ISender`, not `IMediator`.
- `Roles(...)` must use constants from `PearlDesk.Domain.Identity.Roles`, never string literals.
- Map `req` → command manually. Never pass `req` directly to MediatR.
- Always check `result.IsError` and call `SendErrorsAsync` before the happy path.
- Use `Version(1)`.

---

## After creating the endpoint

If this is for a **new module**, remind me to register the endpoint assembly in `Program.cs`:
```csharp
o.Assemblies = [
    // existing...
    typeof(PearlDesk.${moduleName}.Endpoints.${entityName}${input:action}Endpoint).Assembly,
];
```

If this module is already registered in `Program.cs`, no changes are needed — FastEndpoints auto-discovers endpoints within registered assemblies.
