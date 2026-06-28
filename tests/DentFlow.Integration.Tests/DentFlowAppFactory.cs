using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// Shared PostgreSQL container + ASP.NET app factory for all integration tests.
/// One container per test collection run — each test class gets a fresh schema.
/// </summary>
public class DentFlowAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("dentflow_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    // Use a predictable signing key so tests can create their own tokens
    public const string TestJwtKey = "integration-test-secret-key-must-be-at-least-32-chars";
    public const string TestIssuer = "https://mydentflow.com";
    public const string TestAudience = "https://mydentflow.com";
    public const string TestTenantSlug = "testclinic";

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _db.StopAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Replace the real DB connection with the Testcontainer one
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseNpgsql(_db.GetConnectionString()));

            // Migrate the test database
            using var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.Migrate();
        });
    }

    /// <summary>Creates an HTTP client with a valid JWT for the given roles.</summary>
    public HttpClient CreateAuthenticatedClient(string[] roles, string? tenantSlug = null)
    {
        var token = CreateToken(roles, tenantSlug ?? TestTenantSlug);
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            // Host must match Finbuckle's __tenant__.* host strategy
            BaseAddress = new Uri($"http://{tenantSlug ?? TestTenantSlug}.localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public HttpClient CreateAdminClient(string? tenantSlug = null) =>
        CreateAuthenticatedClient(["ClinicOwner", "ClinicAdmin"], tenantSlug);

    public HttpClient CreateReceptionistClient(string? tenantSlug = null) =>
        CreateAuthenticatedClient(["Receptionist"], tenantSlug);

    public HttpClient CreateDentistClient(string? tenantSlug = null) =>
        CreateAuthenticatedClient(["Dentist"], tenantSlug);

    private static string CreateToken(string[] roles, string tenantSlug)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // FastEndpoints sets RoleClaimType = "role" and MapInboundClaims = false,
        // so the short "role" claim name must be used — NOT ClaimTypes.Role (full URL).
        var claims = new List<Claim>
        {
            new("sub", Guid.NewGuid().ToString()),
            new("jti", Guid.NewGuid().ToString()),
            new("tenant", tenantSlug),
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

/// <summary>
/// Shared fixture collection — tests in the same collection share one PostgreSQL container.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationCollection : ICollectionFixture<DentFlowAppFactory> { }
