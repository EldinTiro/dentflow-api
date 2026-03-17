using DentFlow.Identity;
using DentFlow.Tenants;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Finbuckle.MultiTenant;
using DentFlow.Application;
using DentFlow.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console());

// Application + Infrastructure — pass module assemblies for MediatR + FluentValidation scanning
builder.Services.AddApplication(
    typeof(DentFlow.Identity.Application.Commands.RegisterUserCommand).Assembly,
    typeof(DentFlow.Staff.Application.StaffMemberResponse).Assembly,
    typeof(DentFlow.Patients.Application.PatientResponse).Assembly,
    typeof(DentFlow.Appointments.Application.AppointmentResponse).Assembly,
    typeof(DentFlow.Tenants.Application.Commands.CreateTenantCommand).Assembly,
    typeof(DentFlow.Treatments.Application.TreatmentPlanResponse).Assembly
);
builder.Services.AddInfrastructure(builder.Configuration);

// Multi-tenancy — subdomain strategy: {slug}.DentFlow.com
builder.Services
    .AddMultiTenant<TenantInfo>()
    .WithHostStrategy("__tenant__.*")
    .WithConfigurationStore();

// FastEndpoints + JWT (HMAC-SHA256 dev key, swap for RS256 in production)
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? string.Empty;
builder.Services
    .AddFastEndpoints(o =>
    {
        // Endpoint classes live in module assemblies — must be registered explicitly
        o.Assemblies =
        [
            typeof(DentFlow.Identity.Endpoints.LoginEndpoint).Assembly,
            typeof(DentFlow.Staff.Endpoints.StaffCreateEndpoint).Assembly,
            typeof(DentFlow.Patients.Endpoints.PatientCreateEndpoint).Assembly,
            typeof(DentFlow.Appointments.Endpoints.AppointmentBookEndpoint).Assembly,
            typeof(DentFlow.Tenants.Endpoints.TenantCreateEndpoint).Assembly,
            typeof(DentFlow.Treatments.Endpoints.TreatmentPlanCreateEndpoint).Assembly,
        ];
    })
    .AddAuthenticationJwtBearer(o => o.SigningKey = jwtSigningKey)
    .AddAuthorization();

builder.Services.SwaggerDocument(o =>
{
    o.ShortSchemaNames = true;
    o.MaxEndpointVersion = 1;
    o.DocumentSettings = s =>
    {
        s.Title = "DentFlow API";
        s.Version = "v1";
        s.Description = "DentFlow — Multi-tenant Dental Practice Management API";
    };
});

// CORS — allow the frontend dev server and any origins configured in appsettings
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()));

// Rate limiting per tenant
builder.Services.AddRateLimiter(options =>
    options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(
        ctx => System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            ctx.Request.Host.Host,
            _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 300,
                Window = TimeSpan.FromMinutes(1)
            })));

var app = builder.Build();

// Apply EF Core migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DentFlow.Infrastructure.Persistence.ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// Seed roles + SuperAdmin user on startup
await SuperAdminSeeder.SeedAsync(app.Services);
await AppointmentTypeSeeder.SeedAsync(app.Services);

// Health check — before any auth middleware
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseMultiTenant();
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
    c.Serializer.Options.Converters.Add(new JsonStringEnumConverter());
    c.Endpoints.RoutePrefix = "api";
    c.Versioning.Prefix = "v";
    c.Versioning.PrependToRoute = true;
    c.Errors.UseProblemDetails();
});

// Swagger — always enabled for now (restrict to non-Production in future)
app.UseSwaggerGen();

// Redirect root to Swagger UI for convenience
app.MapGet("/", () => Results.Redirect("/swagger"))
   .ExcludeFromDescription()
   .AllowAnonymous();


app.Run();

// Make Program accessible for integration tests
public partial class Program { }

