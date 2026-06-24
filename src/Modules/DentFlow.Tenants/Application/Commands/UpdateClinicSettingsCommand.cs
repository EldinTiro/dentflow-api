using ErrorOr;
using FluentValidation;
using MediatR;
using DentFlow.Domain.Tenants;
using DentFlow.Tenants.Application.Interfaces;

namespace DentFlow.Tenants.Application.Commands;

public record UpdateClinicSettingsCommand(
    Guid TenantId,
    int SlotDurationMinutes,
    string? WeeklyScheduleJson) : IRequest<ErrorOr<ClinicSettingsResponse>>;

public class UpdateClinicSettingsCommandValidator : AbstractValidator<UpdateClinicSettingsCommand>
{
    private static readonly int[] ValidDurations = [15, 20, 30, 45, 60];

    public UpdateClinicSettingsCommandValidator()
    {
        RuleFor(x => x.SlotDurationMinutes)
            .Must(d => ValidDurations.Contains(d))
            .WithMessage("Trajanje termina mora biti 15, 20, 30, 45 ili 60 minuta.");
    }
}

public class UpdateClinicSettingsCommandHandler(ITenantRepository tenantRepository)
    : IRequestHandler<UpdateClinicSettingsCommand, ErrorOr<ClinicSettingsResponse>>
{
    public async Task<ErrorOr<ClinicSettingsResponse>> Handle(UpdateClinicSettingsCommand cmd, CancellationToken ct)
    {
        var tenant = await tenantRepository.GetByIdAsync(cmd.TenantId, ct);
        if (tenant is null) return TenantErrors.NotFound;

        tenant.UpdateWeeklySchedule(cmd.WeeklyScheduleJson ?? string.Empty, cmd.SlotDurationMinutes);
        await tenantRepository.UpdateAsync(tenant, ct);

        return ClinicSettingsResponse.FromEntity(tenant);
    }
}
