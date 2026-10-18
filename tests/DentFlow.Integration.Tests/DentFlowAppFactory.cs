using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Testcontainers.PostgreSql;
using DentFlow.Domain.Identity;
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

    private readonly Dictionary<string, Guid> _tenantIds = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Tenants resolve from the tenants table, so a slug used by a test must exist as a
    /// real row — otherwise nothing resolves and writes fail closed. Seeded on first use
    /// and cached, so existing tests keep working by slug alone.
    /// </summary>
    private Guid EnsureTenant(string slug)
    {
        if (_tenantIds.TryGetValue(slug, out var cached)) return cached;

        var id = SeedTenantAsync(slug, $"Test tenant {slug}").GetAwaiter().GetResult();
        _tenantIds[slug] = id;
        return id;
    }

    /// <summary>Creates an HTTP client with a valid JWT for the given roles.</summary>
    public HttpClient CreateAuthenticatedClient(string[] roles, string? tenantSlug = null)
    {
        var slug = tenantSlug ?? TestTenantSlug;
        var tenantId = EnsureTenant(slug);

        var token = CreateToken(roles, slug, tenantId);
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            // Host must match Finbuckle's __tenant__.* host strategy
            BaseAddress = new Uri($"http://{slug}.localhost")
        });
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Inserts a real tenant row and returns its id. Tenant resolution reads the
    /// tenants table, so isolation tests need actual rows — not just the entries in
    /// appsettings.Testing.json.
    /// </summary>
    public async Task<Guid> SeedTenantAsync(string slug, string name)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.Tenants.FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant());
        if (existing is not null) return existing.Id;

        var tenant = DentFlow.Domain.Tenants.Tenant.Create(slug, name);
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();
        return tenant.Id;
    }

    /// <summary>
    /// Creates a real user with a real password hash, so a test can exercise the actual
    /// login endpoint rather than a minted token. Needed for anything that depends on
    /// what login itself issues — refresh cookies, for instance.
    /// </summary>
    public async Task<Guid> SeedUserAsync(string email, string password, string role, Guid? tenantId)
    {
        using var scope = Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        if (!await roles.RoleExistsAsync(role))
            await roles.CreateAsync(new ApplicationRole { Name = role });

        var existing = await users.FindByEmailAsync(email);
        if (existing is not null) return existing.Id;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            IsActive = true,
            EmailConfirmed = true,
            TenantId = tenantId,
        };

        var result = await users.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException(
                "Could not seed test user: " + string.Join(", ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, role);
        return user.Id;
    }

    /// <summary>
    /// Bare client for a tenant host with no Authorization header — for endpoints that
    /// authenticate themselves, such as login and refresh.
    /// </summary>
    public HttpClient CreateAnonymousClient(string tenantSlug)
    {
        EnsureTenant(tenantSlug);
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://{tenantSlug}.localhost"),
            HandleCookies = false,
        });
    }

    /// <summary>
    /// Client scoped to a specific tenant: carries the tenant id in the tid claim and
    /// addresses the tenant's slug host, so it works under either resolution strategy.
    /// </summary>
    public HttpClient CreateClientForTenant(Guid tenantId, string tenantSlug, params string[] roles)
    {
        var token = CreateToken(roles.Length > 0 ? roles : ["ClinicOwner", "ClinicAdmin"], tenantSlug, tenantId);
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri($"http://{tenantSlug}.localhost")
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

    private static string CreateToken(string[] roles, string tenantSlug, Guid? tenantId = null)
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
        // Production tokens carry the tenant id as "tid"; the claim strategy reads it.
        if (tenantId.HasValue) claims.Add(new Claim("tid", tenantId.Value.ToString()));
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
