using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Appointments.Application.Queries;

public class GetAvailableSlotsQueryHandler(IProviderAvailabilityReader availabilityReader)
    : IRequestHandler<GetAvailableSlotsQuery, ErrorOr<IReadOnlyList<TimeSlot>>>
{
    public async Task<ErrorOr<IReadOnlyList<TimeSlot>>> Handle(
        GetAvailableSlotsQuery query, CancellationToken ct)
    {
        if (query.DurationMinutes < 5)
            return Error.Validation("Slots.InvalidDuration", "Duration must be at least 5 minutes.");

        var slots = await availabilityReader.GetAvailableSlotsAsync(
            query.ProviderId, query.Date, query.DurationMinutes, ct);

        return ErrorOrFactory.From(slots);
    }
}
