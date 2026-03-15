using Microsoft.AspNetCore.Identity;

namespace PearlDesk.Domain.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public Guid? TenantId { get; set; }
    public string? Description { get; set; }
}

