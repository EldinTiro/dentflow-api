using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using PearlDesk.Domain.Identity;

namespace PearlDesk.Identity.Endpoints;

[AllowAnonymous]
public class LoginEndpoint(
    UserManager<ApplicationUser> userManager,
    IConfiguration configuration)
    : Endpoint<LoginRequest, LoginResponse>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Version(1);
        Summary(s =>
        {
            s.Summary = "Login";
            s.Description = "Authenticate with email and password to receive a JWT access token.";
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await userManager.FindByEmailAsync(req.Email);

        if (user is null || !user.IsActive)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, req.Password);
        if (!passwordValid)
        {
            await SendUnauthorizedAsync(ct);
            return;
        }

        var roles = await userManager.GetRolesAsync(user);

        var signingKey = configuration["Jwt:SigningKey"]!;
        var expiryMinutes = int.TryParse(configuration["Jwt:AccessTokenExpiryMinutes"], out var m) ? m : 15;

        var token = JwtBearer.CreateToken(o =>
        {
            o.SigningKey = signingKey;
            o.ExpireAt = DateTime.UtcNow.AddMinutes(expiryMinutes);
            o.User.Roles.AddRange(roles);
            o.User.Claims.Add(("sub", user.Id.ToString()));
            o.User.Claims.Add(("email", user.Email!));
            o.User.Claims.Add(("name", user.FullName));
            if (user.TenantId.HasValue)
                o.User.Claims.Add(("tid", user.TenantId.Value.ToString()));
        });

        user.LastLoginAt = DateTime.UtcNow;
        await userManager.UpdateAsync(user);

        await SendOkAsync(new LoginResponse(
            AccessToken: token,
            ExpiresIn: expiryMinutes * 60,
            Email: user.Email!,
            FullName: user.FullName,
            Roles: [.. roles]
        ), ct);
    }
}

public record LoginRequest(string Email, string Password);

public record LoginResponse(
    string AccessToken,
    int ExpiresIn,
    string Email,
    string FullName,
    List<string> Roles);

