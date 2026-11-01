using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using DentFlow.Domain.Identity;

namespace DentFlow.Integration.Tests;

/// <summary>Uploads over 20 MB are refused by the API itself, with a reason the UI can show.</summary>
[Collection("Integration")]
public class DocumentUploadLimitTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);

    [Fact]
    public async Task AFileOver20MB_Is400_DocumentTooLarge()
    {
        var client = factory.CreateAuthenticatedClient([Roles.Staff]);
        var created = await client.PostAsJsonAsync("/api/v1/patients", new { firstName = "Big", lastName = "Photo", smsOptIn = false, emailOptIn = false });
        var patientId = (await created.Content.ReadFromJsonAsync<IdDto>())!.Id;

        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(new byte[20 * 1024 * 1024 + 1]);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        form.Add(file, "File", "huge.jpg");
        form.Add(new StringContent("Photo"), "Category");

        var response = await client.PostAsync($"/api/v1/patients/{patientId}/documents", form);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().ContainEquivalentOf("Document.TooLarge").And.Contain("larger than 20 MB");
    }
}
