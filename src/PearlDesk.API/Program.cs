using PearlDesk.Identity;
using FastEndpoints;
using FastEndpoints.Security;
using FastEndpoints.Swagger;
using Finbuckle.MultiTenant;
using PearlDesk.Application;
using PearlDesk.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .WriteTo.Console());

// Application + Infrastructure — pass module assemblies for MediatR + FluentValidation scanning
builder.Services.AddApplication(
    typeof(PearlDesk.Identity.Application.Commands.RegisterUserCommand).Assembly,
    typeof(PearlDesk.Staff.Application.StaffMemberResponse).Assembly,
    typeof(PearlDesk.Patients.Application.PatientResponse).Assembly,
    typeof(PearlDesk.Appointments.Application.AppointmentResponse).Assembly
);
builder.Services.AddInfrastructure(builder.Configuration);

// Multi-tenancy — subdomain strategy: {slug}.pearldesk.com
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
            typeof(PearlDesk.Identity.Endpoints.LoginEndpoint).Assembly,
            typeof(PearlDesk.Staff.Endpoints.StaffCreateEndpoint).Assembly,
            typeof(PearlDesk.Patients.Endpoints.PatientCreateEndpoint).Assembly,
            typeof(PearlDesk.Appointments.Endpoints.AppointmentBookEndpoint).Assembly,
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
        s.Title = "PearlDesk API";
        s.Version = "v1";
        s.Description = "PearlDesk — Multi-tenant Dental Practice Management API";
    };
});

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

// Seed roles + SuperAdmin user on startup
await SuperAdminSeeder.SeedAsync(app.Services);

// Health check — before any auth middleware
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
   .AllowAnonymous();

app.UseSerilogRequestLogging();
app.UseRateLimiter();
app.UseMultiTenant();
app.UseAuthentication();
app.UseAuthorization();

app.UseFastEndpoints(c =>
{
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

