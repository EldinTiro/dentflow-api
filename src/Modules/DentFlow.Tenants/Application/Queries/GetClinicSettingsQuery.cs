using ErrorOr;
using MediatR;
using DentFlow.Domain.Tenants;
using DentFlow.Tenants.Application.Interfaces;

namespace DentFlow.Tenants.Application.Queries;

public record GetClinicSettingsQuery(Guid TenantId) : IRequest<ErrorOr<ClinicSettingsResponse>>;

public class GetClinicSettingsQueryHandler(ITenantRepository tenantRepository)
    : IRequestHandler<GetClinicSettingsQuery, ErrorOr<ClinicSettingsResponse>>
{
    public async Task<ErrorOr<ClinicSettingsResponse>> Handle(GetClinicSettingsQuery query, CancellationToken ct)
    {
        var tenant = await tenantRepository.GetByIdAsync(query.TenantId, ct);
        if (tenant is null) return TenantErrors.NotFound;
        return ClinicSettingsResponse.FromEntity(tenant);
    }
}
