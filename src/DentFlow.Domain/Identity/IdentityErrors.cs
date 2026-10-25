using ErrorOr;

namespace DentFlow.Domain.Identity;

public static class IdentityErrors
{
    public static readonly Error InvalidCredentials = Error.Unauthorized(
        "Auth.InvalidCredentials", "Email or password is incorrect.");

    public static readonly Error AccountLocked = Error.Forbidden(
        "Auth.AccountLocked", "Account is locked. Please try again later.");

    public static readonly Error AccountInactive = Error.Forbidden(
        "Auth.AccountInactive", "Account is deactivated.");

    public static readonly Error InvalidRefreshToken = Error.Unauthorized(
        "Auth.InvalidRefreshToken", "Refresh token is invalid or expired.");

    public static readonly Error RefreshTokenReuse = Error.Unauthorized(
        "Auth.RefreshTokenReuse", "Refresh token reuse detected. All sessions have been revoked.");

    public static readonly Error MfaRequired = Error.Unauthorized(
        "Auth.MfaRequired", "Multi-factor authentication is required.");

    public static readonly Error InvalidMfaCode = Error.Validation(
        "Auth.InvalidMfaCode", "The MFA code is invalid or expired.");

    public static readonly Error EmailAlreadyExists = Error.Conflict(
        "Auth.EmailAlreadyExists", "An account with this email already exists.");

    public static readonly Error PasswordChangeRequired = Error.Forbidden(
        "Auth.PasswordChangeRequired", "Please choose a new password before continuing.");

    public static readonly Error PasswordUnchanged = Error.Validation(
        "Auth.PasswordUnchanged", "The new password must be different from the current one.");

    public static readonly Error UserNotFound = Error.NotFound(
        "Auth.UserNotFound", "User not found.");

    // ── User management guards (QA-012) ─────────────────────────────────

    public static readonly Error CannotChangeOwnAccount = Error.Conflict(
        "User.Self", "You can't change your own access here. Use Profile & Settings for your own account.");

    public static readonly Error LastClinicOwner = Error.Conflict(
        "User.LastClinicOwner", "A clinic must keep at least one active owner.");

    public static readonly Error LastSuperAdmin = Error.Conflict(
        "User.LastSuperAdmin", "The platform must keep at least one active SuperAdmin.");

    public static readonly Error OwnerOnly = Error.Forbidden(
        "User.OwnerOnly", "Only a clinic owner can change another owner or grant the owner role.");

    public static readonly Error SuperAdminForClinicUser = Error.Validation(
        "User.SuperAdminForClinicUser", "The SuperAdmin role cannot be given to a clinic user.");
}

