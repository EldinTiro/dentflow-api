using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Interfaces;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Application.Common.Behaviors;

/// <summary>Resolves the patient of an <see cref="IPatientScoped"/> request with the tenant filter.</summary>
public class PatientScopeBehavior<TRequest, TResponse>(IPatientDirectory patients)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    private static readonly Error PatientNotFound = Error.NotFound("Patient.NotFound", "Patient was not found.");

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (request is IPatientScoped scoped && !await patients.ExistsAsync(scoped.PatientId, cancellationToken))
            return (TResponse)(dynamic)new List<Error> { PatientNotFound };

        return await next();
    }
}
