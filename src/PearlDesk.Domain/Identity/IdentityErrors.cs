using ErrorOr;

namespace PearlDesk.Domain.Identity;

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

    public static readonly Error UserNotFound = Error.NotFound(
        "Auth.UserNotFound", "User not found.");
}

