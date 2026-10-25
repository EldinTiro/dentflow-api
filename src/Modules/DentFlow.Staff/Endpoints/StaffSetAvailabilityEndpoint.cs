using FastEndpoints;
using MediatR;
using DentFlow.Staff.Application;
using DentFlow.Staff.Application.Commands;
using DentFlow.Application.Common.Http;
using DentFlow.Domain.Identity;

namespace DentFlow.Staff.Endpoints;

public class StaffSetAvailabilityEndpoint(ISender sender) : Endpoint<SetStaffAvailabilityRequest, StaffAvailabilityResponse>
{
    public override void Configure()
    {
        Post("/staff/{id}/availability");
        Policies(Permission.PeopleManage);
        Version(1);
        Summary(s =>
        {
            s.Summary = "Add an availability slot for a staff member";
            s.Description = "Adds a recurring weekly availability slot for the staff member on the given day.";
        });
    }

    public override async Task HandleAsync(SetStaffAvailabilityRequest req, CancellationToken ct)
    {
        var staffMemberId = Route<Guid>("id");

        var command = new SetStaffAvailabilityCommand(
            staffMemberId,
            req.DayOfWeek,
            req.StartTime,
            req.EndTime,
            req.EffectiveFrom,
            req.EffectiveTo);

        var result = await sender.Send(command, ct);

        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendCreatedAtAsync<StaffGetAvailabilityEndpoint>(new { id = staffMemberId }, result.Value, cancellation: ct);
    }
}

public record SetStaffAvailabilityRequest(
    DayOfWeek DayOfWeek,
    TimeOnly StartTime,
    TimeOnly EndTime,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo);
