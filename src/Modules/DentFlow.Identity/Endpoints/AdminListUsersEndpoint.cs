using FastEndpoints;
using MediatR;
using DomainRoles = DentFlow.Domain.Identity.Roles;
using DentFlow.Identity.Application;
using DentFlow.Identity.Application.Queries;
using DentFlow.Application.Common.Http;

namespace DentFlow.Identity.Endpoints;

public class AdminListUsersEndpoint(ISender sender) : EndpointWithoutRequest<ListUsersResult>
{
    public override void Configure()
    {
        Get("/admin/users");
        Roles(DomainRoles.SuperAdmin);
        Version(1);
        Summary(s => s.Summary = "List all users across tenants (SuperAdmin only)");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var tenantId = Query<Guid?>("tenantId", isRequired: false);
        var role = Query<string?>("role", isRequired: false);
        var searchTerm = Query<string?>("searchTerm", isRequired: false);
        var page = Query<int>("page", isRequired: false);
        var pageSize = Query<int>("pageSize", isRequired: false);

        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var result = await sender.Send(new ListUsersQuery(tenantId, role, searchTerm, page, pageSize), ct);
        if (result.IsError) { await this.SendResultErrorsAsync(result.Errors, ct); return; }
        await SendOkAsync(result.Value, ct);
    }
}
