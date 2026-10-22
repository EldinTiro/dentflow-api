using FastEndpoints;
using MediatR;
using DentFlow.Identity.Application.Commands;
using DentFlow.Identity.Application.Queries;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

// ── GET /users/me ──────────────────────────────────────────────────────────

public class GetUserProfileEndpoint(ISender sender) : EndpointWithoutRequest<UserProfileResult>
{
    public override void Configure()
    {
        Get("/users/me");
        // Every role has a profile; Hygienist, BillingStaff and ReadOnly used to get 403.
        Roles([.. DomainRoles.All]);
        Version(1);
        Summary(s => s.Summary = "Get current user's profile");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var result = await sender.Send(new GetUserProfileQuery(userId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── PUT /users/me ──────────────────────────────────────────────────────────

public record UpdateOwnProfileRequest(string FirstName, string LastName);

public class UpdateOwnProfileEndpoint(ISender sender) : Endpoint<UpdateOwnProfileRequest, UserProfileResult>
{
    public override void Configure()
    {
        Put("/users/me");
        Roles([.. DomainRoles.All]);
        Version(1);
        Summary(s => s.Summary = "Update the current user's name");
    }

    public override async Task HandleAsync(UpdateOwnProfileRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(HttpContext.User.FindFirst("sub")?.Value, out var userId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var result = await sender.Send(new UpdateOwnProfileCommand(userId, req.FirstName, req.LastName), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── GET /users/me/preferences ──────────────────────────────────────────────

public class GetUserPreferencesEndpoint(ISender sender) : EndpointWithoutRequest<UserPreferencesResult>
{
    public override void Configure()
    {
        Get("/users/me/preferences");
        // Every role has a profile; Hygienist, BillingStaff and ReadOnly used to get 403.
        Roles([.. DomainRoles.All]);
        Version(1);
        Summary(s => s.Summary = "Get current user's preferences");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var result = await sender.Send(new GetUserPreferencesQuery(userId), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}

// ── PUT /users/me/preferences ──────────────────────────────────────────────

public class UpsertUserPreferencesRequest
{
    public string Theme { get; init; } = "light";
    public string Language { get; init; } = "en";
    public string TimeFormat { get; init; } = "24h";
    public string DefaultCalendarView { get; init; } = "week";
}

public class UpsertUserPreferencesEndpoint(ISender sender) : Endpoint<UpsertUserPreferencesRequest>
{
    public override void Configure()
    {
        Put("/users/me/preferences");
        // Every role has a profile; Hygienist, BillingStaff and ReadOnly used to get 403.
        Roles([.. DomainRoles.All]);
        Version(1);
        Summary(s => s.Summary = "Update current user's preferences");
    }

    public override async Task HandleAsync(UpsertUserPreferencesRequest req, CancellationToken ct)
    {
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var command = new UpsertUserPreferencesCommand(
            userId,
            req.Theme,
            req.Language,
            req.TimeFormat,
            req.DefaultCalendarView);

        var result = await sender.Send(command, ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}

// ── POST /users/me/change-password ─────────────────────────────────────────

public class ChangePasswordRequest
{
    public string CurrentPassword { get; init; } = default!;
    public string NewPassword { get; init; } = default!;
}

public class ChangePasswordEndpoint(ISender sender) : Endpoint<ChangePasswordRequest>
{
    public override void Configure()
    {
        Post("/users/me/change-password");
        // Every role: a user forced to replace a temporary password must be able to (QA-011).
        Roles([.. DomainRoles.All]);
        Version(1);
        Summary(s => s.Summary = "Change current user's password");
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        var subClaim = HttpContext.User.FindFirst("sub")?.Value;
        if (!Guid.TryParse(subClaim, out var userId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var command = new ChangePasswordCommand(userId, req.CurrentPassword, req.NewPassword);
        var result = await sender.Send(command, ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendNoContentAsync(ct);
    }
}
