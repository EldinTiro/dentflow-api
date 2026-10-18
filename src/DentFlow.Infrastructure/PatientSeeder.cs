using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DentFlow.Patients.Domain;
using DentFlow.Infrastructure.Persistence;

namespace DentFlow.Infrastructure;

public static class PatientSeeder
{
    private static readonly string[] FirstNames =
    [
        "Amir", "Emina", "Alen", "Amira", "Damir", "Dina", "Edin", "Edina", "Faruk", "Fatima",
        "Haris", "Hasnija", "Ibrahim", "Ines", "Jasmin", "Jasmina", "Kemal", "Lamija", "Mario", "Maja",
        "Nedim", "Nermina", "Omar", "Petra", "Rasim", "Sabina", "Senad", "Selma", "Tarik", "Tanja",
        "Adnan", "Almira", "Bojan", "Bojana", "Denis", "Dijana", "Eldin", "Elvira", "Filip", "Goran",
        "Hana", "Ivan", "Ivana", "Jelena", "Josip", "Katarina", "Luka", "Marko", "Marta", "Mirsad",
    ];

    private static readonly string[] LastNames =
    [
        "Aganović", "Babić", "Begić", "Čehić", "Delić", "Đorđević", "Fazlić", "Grbić", "Hadžić", "Husić",
        "Imamović", "Jukić", "Kadić", "Lučić", "Memišević", "Nuhanović", "Omerspahić", "Pašić", "Rašidović",
        "Salković", "Softić", "Terzić", "Ugljanin", "Vukić", "Zukić", "Ahmetović", "Brkić", "Ćatić",
        "Duraković", "Fejzić", "Gušić", "Hodžić", "Ibrahimović", "Jahić", "Karić", "Mahmutović", "Nikolić",
        "Omerović", "Pandžić", "Redžić", "Sinanović", "Topalović", "Umihanić", "Vilić", "Zahirović",
    ];

    private static readonly string[] Cities =
    [
        "Sarajevo", "Mostar", "Tuzla", "Zenica", "Banja Luka", "Bihać", "Travnik", "Goražde",
        "Livno", "Prijedor", "Doboj", "Bijeljina", "Konjic", "Cazin", "Bugojno",
    ];

    private static readonly string[] Occupations =
    [
        "Engineer", "Teacher", "Doctor", "Nurse", "Lawyer", "Accountant", "Architect",
        "Pharmacist", "Student", "Retired", "Entrepreneur", "Electrician", "Chef", null!, null!, null!,
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<TenantInfo>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        var allTenants = await tenantStore.GetAllAsync();

        foreach (var tenant in allTenants)
        {
            // Id is the tenant's Guid; Identifier is the slug.
            if (!Guid.TryParse(tenant.Id, out var tenantId)) continue;

            var existing = await db.Set<Patient>()
                .IgnoreQueryFilters()
                .CountAsync(p => p.TenantId == tenantId);

            if (existing >= 100)
            {
                logger.LogDebug("Patient seed already present for tenant {TenantName} ({Count} patients)", tenant.Name, existing);
                continue;
            }

            var needed = 100 - existing;
            var rng = new Random(42);

            for (var i = 0; i < needed; i++)
            {
                var seq = existing + i + 1;
                var firstName = FirstNames[rng.Next(FirstNames.Length)];
                var lastName  = LastNames[rng.Next(LastNames.Length)];
                var dobYear   = rng.Next(1950, 2010);
                var dobMonth  = rng.Next(1, 13);
                var dobDay    = rng.Next(1, DateTime.DaysInMonth(dobYear, dobMonth) + 1);
                var gender    = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
                var city      = Cities[rng.Next(Cities.Length)];
                var occupation = Occupations[rng.Next(Occupations.Length)];

                var patient = Patient.Create(
                    patientNumber: $"P-{seq:D6}",
                    firstName:     firstName,
                    lastName:      lastName,
                    dateOfBirth:   new DateOnly(dobYear, dobMonth, dobDay),
                    email:         $"{firstName.ToLower()}.{lastName.ToLower().Replace("ć","c").Replace("č","c").Replace("š","s").Replace("ž","z").Replace("đ","dj")}{seq}@example.com",
                    phoneMobile:   $"+38761{rng.Next(1000000, 9999999)}",
                    gender:        gender);

                patient.Update(
                    firstName, lastName,
                    preferredName: null,
                    parentName: null,
                    dateOfBirth: new DateOnly(dobYear, dobMonth, dobDay),
                    gender: gender,
                    pronouns: null,
                    email: $"{firstName.ToLower()}.{lastName.ToLower().Replace("ć","c").Replace("č","c").Replace("š","s").Replace("ž","z").Replace("đ","dj")}{seq}@example.com",
                    phoneMobile: $"+38761{rng.Next(1000000, 9999999)}",
                    phoneHome: null,
                    phoneWork: null,
                    preferredContactMethod: null,
                    addressLine1: $"Ulica br. {rng.Next(1, 200)}",
                    addressLine2: null,
                    city: city,
                    stateProvince: null,
                    postalCode: $"{rng.Next(71000, 79999)}",
                    countryCode: "BA",
                    occupation: occupation,
                    preferredProviderId: null,
                    smsOptIn: false,
                    emailOptIn: true,
                    notes: null);

                patient.SetTenant(tenantId);
                db.Set<Patient>().Add(patient);
            }

            await db.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} patients for tenant {TenantName}", needed, tenant.Name);
        }
    }
}
