FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/DentFlow.API/DentFlow.API.csproj", "src/DentFlow.API/"]
COPY ["src/DentFlow.Application/DentFlow.Application.csproj", "src/DentFlow.Application/"]
COPY ["src/DentFlow.Domain/DentFlow.Domain.csproj", "src/DentFlow.Domain/"]
COPY ["src/DentFlow.Infrastructure/DentFlow.Infrastructure.csproj", "src/DentFlow.Infrastructure/"]
COPY ["src/Modules/DentFlow.Tenants/DentFlow.Tenants.csproj", "src/Modules/DentFlow.Tenants/"]
COPY ["src/Modules/DentFlow.Identity/DentFlow.Identity.csproj", "src/Modules/DentFlow.Identity/"]
COPY ["src/Modules/DentFlow.Staff/DentFlow.Staff.csproj", "src/Modules/DentFlow.Staff/"]
COPY ["src/Modules/DentFlow.Patients/DentFlow.Patients.csproj", "src/Modules/DentFlow.Patients/"]
COPY ["src/Modules/DentFlow.Appointments/DentFlow.Appointments.csproj", "src/Modules/DentFlow.Appointments/"]
COPY ["src/Modules/DentFlow.Treatments/DentFlow.Treatments.csproj", "src/Modules/DentFlow.Treatments/"]
COPY ["src/Modules/DentFlow.Billing/DentFlow.Billing.csproj", "src/Modules/DentFlow.Billing/"]
COPY ["src/Modules/DentFlow.Notifications/DentFlow.Notifications.csproj", "src/Modules/DentFlow.Notifications/"]
COPY ["src/Modules/DentFlow.Documents/DentFlow.Documents.csproj", "src/Modules/DentFlow.Documents/"]
COPY ["src/Modules/DentFlow.Reporting/DentFlow.Reporting.csproj", "src/Modules/DentFlow.Reporting/"]

RUN dotnet restore "src/DentFlow.API/DentFlow.API.csproj"

COPY . .
WORKDIR "/src/src/DentFlow.API"
RUN dotnet build "DentFlow.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DentFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
# The build shown in the app (GET /api/v1/version); CI passes them, a local build leaves them empty
ARG APP_COMMIT=
ARG APP_BUILT_AT=
ENV Build__Commit=$APP_COMMIT     Build__Time=$APP_BUILT_AT
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DentFlow.API.dll"]

