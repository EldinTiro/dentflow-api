using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;

namespace DentFlow.Staff.Endpoints;

public class StaffAddBlockedTimeEndpoint(ISender sender) : Endpoint<AddBlockedTimeRequest, StaffBlockedTimeResponse>
{
    public override void Configure()
    {
        Post("/staff/{id}/blocked-times");
        Roles("ClinicOwner", "SuperAdmin");
        Version(1);
        Summary(s =>
        {
            s.Summary = "Block a time range for a staff member";
            s.Description = "Marks a specific time period as unavailable (e.g. vacation, training, leave).";
        });
    }

    public override async Task HandleAsync(AddBlockedTimeRequest req, CancellationToken ct)
    {
        var staffMemberId = Route<Guid>("id");

        var command = new AddBlockedTimeCommand(
            staffMemberId,
            req.StartAt,
            req.EndAt,
            req.AbsenceType,
            req.Notes);

        var result = await sender.Send(command, ct);

        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendCreatedAtAsync<StaffListBlockedTimesEndpoint>(new { id = staffMemberId }, result.Value, cancellation: ct);
    }
}

public record AddBlockedTimeRequest(
    DateTime StartAt,
    DateTime EndAt,
    string AbsenceType,
    string? Notes);
