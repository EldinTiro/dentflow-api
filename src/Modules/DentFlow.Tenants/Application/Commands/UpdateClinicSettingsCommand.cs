using System.Globalization;
using System.Text.Json;
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
        RuleFor(x => x.WeeklyScheduleJson)
            .Must(OpeningHours.IsValid)
            .When(x => !string.IsNullOrWhiteSpace(x.WeeklyScheduleJson))
            .OverridePropertyName("Clinic.InvalidOpeningHours") // ValidationBehavior uses it as the error code
            .WithMessage("Opening hours: every open day needs a start and an end time (HH:mm), and the end must be after the start.");
    }
}

/// <summary>
/// DT-13: the weekly schedule is the one source of opening hours, so it has to make sense:
/// known day names, HH:mm times, and an end after the start on every open day.
/// </summary>
public static class OpeningHours
{
    private static readonly string[] Days = ["monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday"];

    private sealed record Day(bool IsOpen, string? Start, string? End);

    public static bool IsValid(string? json)
    {
        Dictionary<string, Day>? schedule;
        try
        {
            schedule = JsonSerializer.Deserialize<Dictionary<string, Day>>(json!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return false;
        }
        if (schedule is null) return false;

        foreach (var (name, day) in schedule)
        {
            if (!Days.Contains(name.ToLowerInvariant()) || day is null) return false;
            if (!day.IsOpen) continue;
            if (!TryTime(day.Start, out var start) || !TryTime(day.End, out var end) || end <= start) return false;
        }
        return true;
    }

    private static bool TryTime(string? value, out TimeOnly time) =>
        TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time);
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
