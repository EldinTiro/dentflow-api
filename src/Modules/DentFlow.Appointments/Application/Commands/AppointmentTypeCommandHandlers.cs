using ErrorOr;
using MediatR;
using DentFlow.Appointments.Application.Interfaces;
using DentFlow.Appointments.Domain;

namespace DentFlow.Appointments.Application.Commands;

public class CreateAppointmentTypeCommandHandler(IAppointmentTypeRepository repository)
    : IRequestHandler<CreateAppointmentTypeCommand, ErrorOr<AppointmentTypeResponse>>
{
    public async Task<ErrorOr<AppointmentTypeResponse>> Handle(
        CreateAppointmentTypeCommand command,
        CancellationToken cancellationToken)
    {
        // N-11: the unique index answered a duplicate with a 500
        if (await repository.NameExistsAsync(command.Name, null, cancellationToken))
            return AppointmentErrors.AppointmentTypeDuplicateName;

        var type = AppointmentType.Create(
            command.Name,
            command.DefaultDurationMinutes,
            command.Description,
            command.ColorHex,
            command.IsBookableOnline,
            command.DefaultFee);

        await repository.AddAsync(type, cancellationToken);

        return AppointmentTypeResponse.FromEntity(type);
    }
}

public class UpdateAppointmentTypeCommandHandler(IAppointmentTypeRepository repository)
    : IRequestHandler<UpdateAppointmentTypeCommand, ErrorOr<AppointmentTypeResponse>>
{
    public async Task<ErrorOr<AppointmentTypeResponse>> Handle(
        UpdateAppointmentTypeCommand command,
        CancellationToken cancellationToken)
    {
        var type = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (type is null)
            return AppointmentErrors.AppointmentTypeNotFound;

        if (await repository.NameExistsAsync(command.Name, type.Id, cancellationToken))
            return AppointmentErrors.AppointmentTypeDuplicateName;

        type.Update(
            command.Name,
            command.DefaultDurationMinutes,
            command.Description,
            command.ColorHex,
            command.IsBookableOnline,
            command.DefaultFee);

        await repository.UpdateAsync(type, cancellationToken);

        return AppointmentTypeResponse.FromEntity(type);
    }
}

public class DeleteAppointmentTypeCommandHandler(IAppointmentTypeRepository repository)
    : IRequestHandler<DeleteAppointmentTypeCommand, ErrorOr<Deleted>>
{
    public async Task<ErrorOr<Deleted>> Handle(
        DeleteAppointmentTypeCommand command,
        CancellationToken cancellationToken)
    {
        var type = await repository.GetByIdAsync(command.Id, cancellationToken);
        if (type is null)
            return AppointmentErrors.AppointmentTypeNotFound;

        await repository.SoftDeleteAsync(type, cancellationToken);

        return Result.Deleted;
    }
}
