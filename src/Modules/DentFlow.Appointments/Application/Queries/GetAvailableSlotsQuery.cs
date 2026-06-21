using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;

namespace DentFlow.Appointments.Application.Queries;

public record GetAvailableSlotsQuery(
    Guid ProviderId,
    DateOnly Date,
    int DurationMinutes = 30) : IRequest<ErrorOr<IReadOnlyList<TimeSlot>>>;
