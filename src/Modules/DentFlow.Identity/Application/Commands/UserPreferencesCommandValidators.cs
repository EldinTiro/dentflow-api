using FluentValidation;

namespace DentFlow.Identity.Application.Commands;

public class UpsertUserPreferencesCommandValidator : AbstractValidator<UpsertUserPreferencesCommand>
{
    private static readonly string[] AllowedThemes = ["light", "dark"];
    private static readonly string[] AllowedLanguages = ["en", "bs", "de"];
    private static readonly string[] AllowedTimeFormats = ["12h", "24h"];
    private static readonly string[] AllowedCalendarViews = ["day", "week", "month"];

    public UpsertUserPreferencesCommandValidator()
    {
        RuleFor(x => x.Theme)
            .NotEmpty()
            .Must(v => AllowedThemes.Contains(v))
            .WithMessage("Theme must be 'light' or 'dark'.");

        RuleFor(x => x.Language)
            .NotEmpty()
            .Must(v => AllowedLanguages.Contains(v))
            .WithMessage("Language must be 'en', 'bs', or 'de'.");

        RuleFor(x => x.TimeFormat)
            .NotEmpty()
            .Must(v => AllowedTimeFormats.Contains(v))
            .WithMessage("TimeFormat must be '12h' or '24h'.");

        RuleFor(x => x.DefaultCalendarView)
            .NotEmpty()
            .Must(v => AllowedCalendarViews.Contains(v))
            .WithMessage("DefaultCalendarView must be 'day', 'week', or 'month'.");
    }
}

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(12).WithMessage("Password must be at least 12 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one non-alphanumeric character.");
    }
}
