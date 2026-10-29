﻿﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DentFlow.Patients.Domain;

namespace DentFlow.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.PatientNumber).HasMaxLength(20).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PreferredName).HasMaxLength(100);
        builder.Property(p => p.ParentName).HasMaxLength(150);
        builder.Property(p => p.Gender)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(p => p.Pronouns).HasMaxLength(50);
        builder.Property(p => p.MaritalStatus).HasMaxLength(30);
        builder.Property(p => p.Occupation).HasMaxLength(100);
        builder.Property(p => p.PhoneMobile).HasMaxLength(30);
        builder.Property(p => p.PhoneHome).HasMaxLength(30);
        builder.Property(p => p.PhoneWork).HasMaxLength(30);
        builder.Property(p => p.Email).HasMaxLength(255);
        builder.Property(p => p.PreferredContactMethod)
            .HasConversion<string>()
            .HasMaxLength(20);
        builder.Property(p => p.AddressLine1).HasMaxLength(255);
        builder.Property(p => p.AddressLine2).HasMaxLength(255);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.StateProvince).HasMaxLength(100);
        builder.Property(p => p.PostalCode).HasMaxLength(20);
        builder.Property(p => p.CountryCode).HasColumnType("char(2)");
        builder.Property(p => p.LanguagePreference).HasMaxLength(10);
        builder.Property(p => p.ReferredBySource).HasMaxLength(100);
        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.PatientNumber }).IsUnique();
        builder.HasIndex(p => new { p.TenantId, p.LastName, p.FirstName });
        builder.HasIndex(p => new { p.TenantId, p.Email })
            .HasFilter("\"Email\" IS NOT NULL AND \"IsDeleted\" = false");

        builder.HasMany(p => p.EmergencyContacts)
            .WithOne(e => e.Patient)
            .HasForeignKey(e => e.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Allergies)
            .WithOne(a => a.Patient)
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.MedicalHistories)
            .WithOne(m => m.Patient)
            .HasForeignKey(m => m.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ToothFindingConfiguration : IEntityTypeConfiguration<ToothFinding>
{
    public void Configure(EntityTypeBuilder<ToothFinding> builder)
    {
        builder.ToTable("tooth_findings");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Condition).HasMaxLength(20).IsRequired();
        builder.Property(f => f.Surfaces).HasMaxLength(5);
        builder.Property(f => f.Notes).HasMaxLength(500);
        builder.HasIndex(f => new { f.TenantId, f.PatientId });
        builder.HasOne<Patient>()
            .WithMany()
            .HasForeignKey(f => f.PatientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class AllergyConfiguration : IEntityTypeConfiguration<Allergy>
{
    public void Configure(EntityTypeBuilder<Allergy> builder)
    {
        builder.ToTable("allergies");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Allergen).HasMaxLength(255).IsRequired();
        builder.Property(a => a.Reaction).HasMaxLength(255);
        builder.Property(a => a.Severity).HasMaxLength(20);
    }
}

public class PatientEmergencyContactConfiguration : IEntityTypeConfiguration<PatientEmergencyContact>
{
    public void Configure(EntityTypeBuilder<PatientEmergencyContact> builder)
    {
        builder.ToTable("patient_emergency_contacts");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(255).IsRequired();
        builder.Property(e => e.Relationship).HasMaxLength(50);
        builder.Property(e => e.PhonePrimary).HasMaxLength(30);
        builder.Property(e => e.PhoneSecondary).HasMaxLength(30);
        builder.Property(e => e.Email).HasMaxLength(255);
    }
}

public class MedicalHistoryConfiguration : IEntityTypeConfiguration<MedicalHistory>
{
    public void Configure(EntityTypeBuilder<MedicalHistory> builder)
    {
        builder.ToTable("medical_histories");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.BloodType).HasMaxLength(5);
        builder.Property(m => m.RecordedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(m => m.PhysicianName).HasMaxLength(255);
        builder.Property(m => m.PhysicianPhone).HasMaxLength(30);
    }
}

public class PatientDocumentConfiguration : IEntityTypeConfiguration<PatientDocument>
{
    public void Configure(EntityTypeBuilder<PatientDocument> builder)
    {
        builder.ToTable("patient_documents");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.FileName).HasMaxLength(500).IsRequired();
        builder.Property(d => d.ContentType).HasMaxLength(200).IsRequired();
        builder.Property(d => d.StorageKey).HasMaxLength(1000).IsRequired();
        builder.Property(d => d.Category).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Notes).HasMaxLength(1000);
        builder.HasIndex(d => new { d.TenantId, d.PatientId });
    }
}

