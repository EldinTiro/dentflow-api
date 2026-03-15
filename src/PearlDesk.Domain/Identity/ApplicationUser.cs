using Microsoft.AspNetCore.Identity;

namespace PearlDesk.Domain.Identity;

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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FullName => $"{FirstName} {LastName}";
}

