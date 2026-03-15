using FastEndpoints;
using MediatR;
using PearlDesk.Staff.Application;
using PearlDesk.Staff.Application.Commands;
using PearlDesk.Staff.Domain;

namespace PearlDesk.Staff.Endpoints;

public class StaffCreateEndpoint(ISender sender) : Endpoint<CreateStaffMemberRequest, StaffMemberResponse>
{
    public override void Configure()
    {
        Post("/staff");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Create a new staff member";
            s.Description = "Creates a new staff member record for the current tenant";
        });
    }

    public override async Task HandleAsync(CreateStaffMemberRequest req, CancellationToken ct)
    {
        var command = new CreateStaffMemberCommand(
            req.StaffType, req.FirstName, req.LastName, req.Email,
            req.Phone, req.HireDate, req.Specialty, req.ColorHex);

        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            await SendErrorsAsync(cancellation: ct);
            return;
        }
        await SendCreatedAtAsync<StaffGetByIdEndpoint>(new { id = result.Value.Id }, result.Value, cancellation: ct);
    }
}

public record CreateStaffMemberRequest(
    StaffType StaffType,
    string FirstName,
    string LastName,
    string? Email,
    string? Phone,
    DateOnly? HireDate,
    string? Specialty,
    string? ColorHex);
