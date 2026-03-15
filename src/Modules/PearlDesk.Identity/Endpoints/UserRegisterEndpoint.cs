using FastEndpoints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using PearlDesk.Identity.Application.Commands;
using AppRoles = PearlDesk.Domain.Identity.Roles;

namespace PearlDesk.Identity.Endpoints;

/// <summary>
/// Registers a new user account.
/// Open only to SuperAdmin globally, or ClinicOwner within their tenant.
/// </summary>
[AllowAnonymous] // temporarily open; restrict to SuperAdmin / ClinicOwner in production gate
public class UserRegisterEndpoint(ISender sender)
    : Endpoint<RegisterUserRequest, RegisterUserResponse>
{
    public override void Configure()
    {
        Post("/auth/register");
        AllowAnonymous();
        Version(1);
        Summary(s =>
        {
            s.Summary = "Register a new user";
            s.Description = "Creates a new user account and assigns the specified role. " +
                            "In production this endpoint must be restricted to SuperAdmin or ClinicOwner.";
        });
    }

    public override async Task HandleAsync(RegisterUserRequest req, CancellationToken ct)
    {
        var command = new RegisterUserCommand(
            req.Email,
            req.Password,
            req.FirstName,
            req.LastName,
            req.Role ?? AppRoles.Receptionist,
            req.TenantId);

        var result = await sender.Send(command, ct);

        if (result.IsError)
        {
            AddError(result.FirstError.Description);
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var value = result.Value;
        await SendCreatedAtAsync<UserRegisterEndpoint>(
            new { },
            new RegisterUserResponse(value.UserId, value.Email, value.FullName, value.Role),
            generateAbsoluteUrl: false,
            cancellation: ct);
    }
}

public record RegisterUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? Role,
    Guid? TenantId);

public record RegisterUserResponse(
    Guid UserId,
    string Email,
    string FullName,
    string Role);
