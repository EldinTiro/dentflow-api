namespace DentFlow.Domain.Identity;

/// <summary>Custom JWT claim names shared by token issuance and the API pipeline.</summary>
public static class IdentityClaims
{
    /// <summary>Present (value "true") while the user still has to replace a temporary password.</summary>
    public const string PasswordChangeRequired = "pwd_change";
}
