using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Commands;
using DentFlow.Staff.Domain;
using DentFlow.Application.Common.Http;

namespace DentFlow.Staff.Endpoints;

public class StaffCreateEndpoint(ISender sender) : Endpoint<CreateStaffMemberRequest, StaffMemberResponse>
{
    public override void Configure()
    {
        Post("/staff");
        Roles("ClinicOwner", "SuperAdmin");
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
            await this.SendResultErrorsAsync(result.Errors, ct);
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
