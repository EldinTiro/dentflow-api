namespace DentFlow.Application.Common.Requests;

/// <summary>
/// A request about one patient. <see cref="Behaviors.PatientScopeBehavior{TRequest, TResponse}"/>
/// answers 404 <c>Patient.NotFound</c> before the handler runs unless that patient exists in
/// the caller's clinic (DT-01/02: child records were attached to other clinics' patients and
/// to ids that don't exist, because handlers only used the id as a foreign key).
/// </summary>
public interface IPatientScoped
{
    Guid PatientId { get; }
}
