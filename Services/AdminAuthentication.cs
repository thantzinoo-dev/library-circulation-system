using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using School_Library_Management.Models;

namespace School_Library_Management.Services;

public static class AdminAuthentication
{
    public const string MustChangePasswordClaim = "must_change_password";

    public static ClaimsPrincipal CreatePrincipal(AdminAccount account)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, "Administrator"),
            new(MustChangePasswordClaim, account.MustChangePassword ? "true" : "false")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
