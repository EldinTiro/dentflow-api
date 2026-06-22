using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Domain.Tenants;
using DentFlow.Tenants.Application.Interfaces;

namespace DentFlow.Tenants.Application.Queries;

public record GetTenantFeaturesQuery(Guid TenantId) : IRequest<ErrorOr<TenantFeaturesResponse>>;

public class GetTenantFeaturesQueryHandler(
    ITenantRepository tenantRepository,
    IFeatureService featureService)
    : IRequestHandler<GetTenantFeaturesQuery, ErrorOr<TenantFeaturesResponse>>
{
    public async Task<ErrorOr<TenantFeaturesResponse>> Handle(
        GetTenantFeaturesQuery query,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(query.TenantId, cancellationToken);
        if (tenant is null) return TenantErrors.NotFound;

        var flags = featureService.EnabledFlags()
            .Select(f => f.ToString())
            .ToList();

        var quotas = featureService.AllQuotas();

        return new TenantFeaturesResponse(tenant.Plan, flags, quotas);
    }
}
