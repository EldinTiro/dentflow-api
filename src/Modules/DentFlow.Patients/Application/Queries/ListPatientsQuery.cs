﻿using ErrorOr;
using MediatR;
using DentFlow.Patients.Domain;

namespace DentFlow.Patients.Application.Queries;

public record ListPatientsQuery(
    string? SearchTerm = null,
    PatientStatus? Status = null,
    string? RecallFilter = null,
    int Page = 1,
    int PageSize = 20) : IRequest<ErrorOr<PagedResult<PatientResponse>>>;

