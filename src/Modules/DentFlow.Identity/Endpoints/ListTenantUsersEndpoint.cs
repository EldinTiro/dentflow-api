using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Identity.Application;
using DentFlow.Identity.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class ListTenantUsersEndpoint(ISender sender) : EndpointWithoutRequest<ListUsersResult>
{
    public override void Configure()
    {
        Get("/users");
        Roles(DomainRoles.ClinicOwner, DomainRoles.ClinicAdmin);
        Version(1);
        Summary(s => s.Summary = "List users belonging to the caller's clinic");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tidClaim = HttpContext.User.FindFirst("tid")?.Value;
        if (!Guid.TryParse(tidClaim, out var tenantId))
        {
            await SendForbiddenAsync(ct);
            return;
        }

        var search = Query<string?>("search", isRequired: false);
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await sender.Send(new ListTenantUsersQuery(tenantId, search, page, pageSize), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
