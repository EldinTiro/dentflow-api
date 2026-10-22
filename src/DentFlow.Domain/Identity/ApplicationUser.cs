using Microsoft.AspNetCore.Identity;

namespace DentFlow.Domain.Identity;

/// <summary>
/// Extended ASP.NET Core Identity user with tenant awareness.
/// Linked 1:1 to either a staff_member or a patient record.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    public Guid? TenantId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Set when someone else chose the password (invite, provisioning, admin reset).
    /// Until the user picks their own, the API only lets them change it (QA-011).
    /// </summary>
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}";
}

