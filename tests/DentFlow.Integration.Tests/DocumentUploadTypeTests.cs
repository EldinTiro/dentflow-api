using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Domain.Identity;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>
/// QA pass 2026-10-31 (N-06): an HTML page, a scripted SVG and an .exe were stored as patient
/// documents and served back by a download link. Only PDFs, photos and Word files are kept.
/// </summary>
[Collection("Integration")]
public class DocumentUploadTypeTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    /// <summary>Document storage is a paid feature, so the check needs a clinic on a plan that has it.</summary>
    private async Task<HttpClient> DocumentClinicClientAsync()
    {
        const string slug = "upload-type-clinic";
        var tenantId = await factory.SeedTenantAsync(slug, "Upload Type Clinic");
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var tenant = await db.Tenants.SingleAsync(t => t.Id == tenantId);
            tenant.SetPlan("Enterprise", null);
            await db.SaveChangesAsync();
        }
        return factory.CreateClientForTenant(tenantId, slug, Roles.Staff);
    }

    [Theory]
    [InlineData("evil.html", "text/html", "<script>alert(1)</script>")]
    [InlineData("evil.pdf", "application/pdf", "<script>alert(1)</script>")]
    [InlineData("evil.svg", "image/svg+xml", "<svg xmlns=\"http://www.w3.org/2000/svg\" onload=\"alert(1)\"/>")]
    [InlineData("setup.exe", "application/octet-stream", "MZ fake")]
    public async Task AFileThatIsNotADocumentOrPhoto_Is400_UnsupportedType(string name, string type, string content)
    {
        var client = await DocumentClinicClientAsync();
        var created = await client.PostAsJsonAsync("/api/v1/patients", new { firstName = "Bad", lastName = "File" });
        var patientId = (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.ASCII.GetBytes(content));
        file.Headers.ContentType = new MediaTypeHeaderValue(type);
        form.Add(file, "File", name);
        form.Add(new StringContent("Other"), "Category");

        var response = await client.PostAsync($"/api/v1/patients/{patientId}/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().ContainEquivalentOf("Document.UnsupportedType");
    }
}
