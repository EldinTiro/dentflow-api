using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Staff.Endpoints;

public class StaffUpdateEndpoint(ISender sender) : Endpoint<UpdateStaffMemberRequest, StaffMemberResponse>
{
    public override void Configure()
    {
        Put("/staff/{id}");
        Roles("ClinicOwner", "ClinicAdmin", "SuperAdmin");
        Version(1);
        Summary(s => s.Summary = "Update a staff member");
    }

    public override async Task HandleAsync(UpdateStaffMemberRequest req, CancellationToken ct)
    {
        var id = Route<Guid>("id");
        var command = new UpdateStaffMemberCommand(
            id, req.FirstName, req.LastName, req.Email, req.Phone, req.Specialty,
            req.ColorHex, req.Biography, req.LicenseNumber, req.LicenseExpiry, req.NpiNumber,
            req.Address, req.City, req.PostalCode);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

public record UpdateStaffMemberRequest(
    string FirstName, string LastName, string? Email, string? Phone,
    string? Specialty, string? ColorHex, string? Biography,
    string? LicenseNumber, DateOnly? LicenseExpiry, string? NpiNumber,
    string? Address, string? City, string? PostalCode);
