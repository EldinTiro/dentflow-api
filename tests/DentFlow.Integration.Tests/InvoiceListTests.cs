using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using DentFlow.Billing.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Integration.Tests;

/// <summary>QA-025: the invoice list had no patient and no overdue state.</summary>
[Collection("Integration")]
public class InvoiceListTests(DentFlowAppFactory factory)
{
    private record IdDto(Guid Id);
    private record SummaryDto(Guid Id, string PatientName, bool IsOverdue);
    private record Paged(List<SummaryDto> Items);

    [Fact]
    public async Task List_ShowsThePatientName_AndFlagsOverdueInvoices()
    {
        var client = factory.CreateAdminClient();
        var patient = await client.PostAsJsonAsync("/api/v1/patients", new
        {
            firstName = "Selma", lastName = "Aganović", smsOptIn = false, emailOptIn = false
        });
        var patientId = (await patient.Content.ReadFromJsonAsync<IdDto>())!.Id;

        async Task<Guid> InvoiceDue(DateTime due)
        {
            var r = await client.PostAsJsonAsync($"/api/v1/patients/{patientId}/invoices", new
            {
                dueDate = due,
                lineItems = new[] { new { description = "Exam", quantity = 1, unitFee = 50m } }
            });
            r.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await r.Content.ReadFromJsonAsync<IdDto>())!.Id;
        }
        var overdue = await InvoiceDue(DateTime.UtcNow.AddDays(1));
        var current = await InvoiceDue(DateTime.UtcNow.AddDays(10));
        // The API (rightly) refuses a due date in the past; age one invoice directly.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoice = await db.Set<Invoice>().IgnoreQueryFilters().SingleAsync(i => i.Id == overdue);
            invoice.Update(DateTime.UtcNow.AddDays(-3), null);
            await db.SaveChangesAsync();
        }

        var list = await client.GetFromJsonAsync<Paged>($"/api/v1/patients/{patientId}/invoices");

        list!.Items.Should().OnlyContain(i => i.PatientName == "Selma Aganović");
        list.Items.Single(i => i.Id == overdue).IsOverdue.Should().BeTrue();
        list.Items.Single(i => i.Id == current).IsOverdue.Should().BeFalse();
    }
}
