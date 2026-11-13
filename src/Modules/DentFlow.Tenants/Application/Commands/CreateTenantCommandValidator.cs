using FluentValidation;

namespace DentFlow.Tenants.Application.Commands;

public class CreateTenantCommandValidator : AbstractValidator<CreateTenantCommand>
{
    private static readonly System.Text.RegularExpressions.Regex SlugRegex =
        new(@"^[a-z0-9]+(?:-[a-z0-9]+)*$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>
    /// The slug is the clinic's subdomain. These belong to the website (mydentflow.com, www),
    /// the app (app.mydentflow.com) and services we may add (user decision 2026-11-13).
    /// </summary>
    public static readonly IReadOnlySet<string> Reserved = new HashSet<string>
    {
        "app", "www", "api", "admin", "mail", "email", "smtp", "ftp", "status", "help", "support",
        "docs", "blog", "static", "assets", "cdn", "dev", "test", "staging", "demo", "portal", "dashboard",
    };

    public CreateTenantCommandValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty()
            .MinimumLength(2).MaximumLength(63)
            .Must(s => SlugRegex.IsMatch(s))
            .WithMessage("Slug must be lowercase letters, numbers and hyphens only (e.g. 'bright-smile').")
            .Must(s => !Reserved.Contains(s))
            .WithMessage("This name is reserved. Choose another one for the clinic.");

        RuleFor(x => x.Name)
            .NotEmpty().MaximumLength(200);

        RuleFor(x => x.Plan)
            .NotEmpty()
            .Must(p => p is "Free" or "Pro" or "Enterprise")
            .WithMessage("Plan must be Free, Pro, or Enterprise.");

        RuleFor(x => x.OwnerEmail)
            .NotEmpty().EmailAddress().MaximumLength(256);

        RuleFor(x => x.OwnerFirstName)
            .NotEmpty().MaximumLength(100);

        RuleFor(x => x.OwnerLastName)
            .NotEmpty().MaximumLength(100);
    }
}
