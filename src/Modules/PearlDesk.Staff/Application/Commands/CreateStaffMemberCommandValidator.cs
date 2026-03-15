﻿using FluentValidation;

namespace PearlDesk.Staff.Application.Commands;

public class CreateStaffMemberCommandValidator : AbstractValidator<CreateStaffMemberCommand>
{
    public CreateStaffMemberCommandValidator()
    {

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(255)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Phone)
            .MaximumLength(30)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.ColorHex)
            .Matches(@"^#[0-9A-Fa-f]{6}$")
            .WithMessage("ColorHex must be a valid hex colour (e.g. #3B82F6)")
            .When(x => !string.IsNullOrWhiteSpace(x.ColorHex));
    }
}

