FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["src/PearlDesk.API/PearlDesk.API.csproj", "src/PearlDesk.API/"]
COPY ["src/PearlDesk.Application/PearlDesk.Application.csproj", "src/PearlDesk.Application/"]
COPY ["src/PearlDesk.Domain/PearlDesk.Domain.csproj", "src/PearlDesk.Domain/"]
COPY ["src/PearlDesk.Infrastructure/PearlDesk.Infrastructure.csproj", "src/PearlDesk.Infrastructure/"]
COPY ["src/Modules/PearlDesk.Tenants/PearlDesk.Tenants.csproj", "src/Modules/PearlDesk.Tenants/"]
COPY ["src/Modules/PearlDesk.Identity/PearlDesk.Identity.csproj", "src/Modules/PearlDesk.Identity/"]
COPY ["src/Modules/PearlDesk.Staff/PearlDesk.Staff.csproj", "src/Modules/PearlDesk.Staff/"]
COPY ["src/Modules/PearlDesk.Patients/PearlDesk.Patients.csproj", "src/Modules/PearlDesk.Patients/"]
COPY ["src/Modules/PearlDesk.Appointments/PearlDesk.Appointments.csproj", "src/Modules/PearlDesk.Appointments/"]
COPY ["src/Modules/PearlDesk.Treatments/PearlDesk.Treatments.csproj", "src/Modules/PearlDesk.Treatments/"]
COPY ["src/Modules/PearlDesk.Billing/PearlDesk.Billing.csproj", "src/Modules/PearlDesk.Billing/"]
COPY ["src/Modules/PearlDesk.Notifications/PearlDesk.Notifications.csproj", "src/Modules/PearlDesk.Notifications/"]
COPY ["src/Modules/PearlDesk.Documents/PearlDesk.Documents.csproj", "src/Modules/PearlDesk.Documents/"]
COPY ["src/Modules/PearlDesk.Reporting/PearlDesk.Reporting.csproj", "src/Modules/PearlDesk.Reporting/"]

RUN dotnet restore "src/PearlDesk.API/PearlDesk.API.csproj"

COPY . .
WORKDIR "/src/src/PearlDesk.API"
RUN dotnet build "PearlDesk.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PearlDesk.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PearlDesk.API.dll"]

