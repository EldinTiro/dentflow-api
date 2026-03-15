---
mode: agent
description: Add a new command (and optionally query) to an existing PearlDesk module.
---

Add the files listed below to the specified module. Follow all conventions in `.github/copilot-instructions.md` and `src/Modules/.instructions.md`. Use `PearlDesk.Patients` as the canonical reference.

**Module**: ${input:moduleName:e.g. Patients}
**Entity**: ${input:entityName:e.g. Patient}
**Command name** (verb): ${input:commandVerb:e.g. Archive, Discharge, Confirm}
**What it does**: ${input:description:Brief description of the command's intent}
**Return type**: ${input:returnType:e.g. PatientResponse, Updated, Deleted}

---

## Files to create

### 1. Command record (`Application/Commands/${commandVerb}${entityName}Command.cs`)

```csharp
public record ${commandVerb}${entityName}Command(
    Guid Id
    // Add any additional parameters needed
) : IRequest<ErrorOr<${returnType}>>;
```

### 2. Command handler (`Application/Commands/${commandVerb}${entityName}CommandHandler.cs`)

Follow this pattern:
```csharp
public class ${commandVerb}${entityName}CommandHandler(I${entityName}Repository repository)
    : IRequestHandler<${commandVerb}${entityName}Command, ErrorOr<${returnType}>>
{
    public async Task<ErrorOr<${returnType}>> Handle(
        ${commandVerb}${entityName}Command command, CancellationToken ct)
    {
        // 1. Fetch the entity — return NotFound error if null
        // 2. Apply domain method / domain state change
        // 3. Persist via repository
        // 4. Return appropriate response / Result.Updated / Result.Deleted
    }
}
```

### 3. Validator (`Application/Commands/${commandVerb}${entityName}CommandValidator.cs`)

```csharp
public class ${commandVerb}${entityName}CommandValidator : AbstractValidator<${commandVerb}${entityName}Command>
{
    public ${commandVerb}${entityName}CommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        // Add any additional rules
    }
}
```

### 4. Domain method (in `Domain/${entityName}.cs`) — if needed

If the command requires a new state transition or mutation on the entity, add a domain method:
```csharp
public void ${commandVerb}(/* params */)
{
    // mutation logic
    SetUpdated();  // ← always call at the end
}
```

### 5. Error constant (in `Domain/${entityName}Errors.cs`) — if a new error is needed

```csharp
public static readonly Error Invalid${commandVerb} = Error.Validation(
    "${entityName}.Invalid${commandVerb}",
    "Cannot perform this action in the current state.");
```

### 6. Unit test (`tests/PearlDesk.${moduleName}.Tests/Commands/${commandVerb}${entityName}CommandHandlerTests.cs`)

Write tests for:
- Happy path — command succeeds, correct response returned
- Not-found path — entity doesn't exist, returns `${entityName}Errors.NotFound`
- Any invalid-state path — returns appropriate validation error
- Verify write methods were called (`.Received(1)`)

---

After creating these files, no changes to `Program.cs` or `DependencyInjection.cs` are needed — command handlers are auto-discovered by MediatR assembly scanning.
