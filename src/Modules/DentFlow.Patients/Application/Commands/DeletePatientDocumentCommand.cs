using ErrorOr;
using MediatR;
using DentFlow.Application.Common.Requests;

namespace DentFlow.Patients.Application.Commands;

public record DeletePatientDocumentCommand(
    Guid PatientId,
    Guid DocumentId) : IPatientScoped, IRequest<ErrorOr<Deleted>>;
