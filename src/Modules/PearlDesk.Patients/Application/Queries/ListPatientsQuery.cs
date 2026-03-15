﻿using ErrorOr;
using MediatR;
using PearlDesk.Patients.Domain;

namespace PearlDesk.Patients.Application.Queries;

public record ListPatientsQuery(
    string? SearchTerm = null,
    PatientStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<ErrorOr<PagedResult<PatientResponse>>>;

