using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;
using School_Library_Management.Services;

namespace School_Library_Management.Pages;

public class LoginModel(ApplicationDbContext context) : PageModel
{
    private const int MaximumFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);
    private readonly ApplicationDbContext _context = context;
    private readonly PasswordHasher<AdminAccount> _passwordHasher = new();

    [BindProperty, Required(ErrorMessage = "Enter your email or username.")]
    public string? UsernameOrEmail { get; set; }

    [BindProperty, Required(ErrorMessage = "Enter your password.")]
    public string? Password { get; set; }

    [BindProperty]
    public bool RememberMe { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public IActionResult? OnGet()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        if (User.HasClaim(AdminAuthentication.MustChangePasswordClaim, "true"))
        {
            return RedirectToPage("/Profile/Password", new { firstLogin = true });
        }

        return RedirectToLocalPage();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        UsernameOrEmail = UsernameOrEmail?.Trim();
        if (!ModelState.IsValid)
        {
            Password = null;
            return Page();
        }

        var normalizedLogin = AdminAuthentication.Normalize(UsernameOrEmail!);
        var account = await _context.AdminAccounts.SingleOrDefaultAsync(item =>
            item.NormalizedUsername == normalizedLogin || item.NormalizedEmail == normalizedLogin);

        if (account is null || !account.IsActive)
        {
            return InvalidLogin();
        }

        var now = DateTime.UtcNow;
        if (account.LockoutEndUtc is not null && account.LockoutEndUtc > now)
        {
            Password = null;
            ModelState.AddModelError(string.Empty, "This account is temporarily locked. Try again in 15 minutes.");
            return Page();
        }

        if (account.LockoutEndUtc is not null)
        {
            account.LockoutEndUtc = null;
            account.FailedLoginAttempts = 0;
        }

        var submittedPassword = Password!;
        var verification = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, submittedPassword);
        Password = null;
        if (verification == PasswordVerificationResult.Failed)
        {
            account.FailedLoginAttempts++;
            if (account.FailedLoginAttempts >= MaximumFailedAttempts)
            {
                account.FailedLoginAttempts = 0;
                account.LockoutEndUtc = now.Add(LockoutDuration);
            }

            account.UpdatedAtUtc = now;
            await _context.SaveChangesAsync();
            return InvalidLogin();
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _passwordHasher.HashPassword(account, submittedPassword);
        }

        account.FailedLoginAttempts = 0;
        account.LockoutEndUtc = null;
        account.LastLoginAtUtc = now;
        account.UpdatedAtUtc = now;
        await _context.SaveChangesAsync();

        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = RememberMe,
            AllowRefresh = true
        };
        if (RememberMe)
        {
            authenticationProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AdminAuthentication.CreatePrincipal(account),
            authenticationProperties);

        if (account.MustChangePassword)
        {
            return RedirectToPage("/Profile/Password", new { firstLogin = true });
        }

        return RedirectToLocalPage();
    }

    private IActionResult InvalidLogin()
    {
        Password = null;
        ModelState.AddModelError(string.Empty, "The email/username or password is incorrect.");
        return Page();
    }

    private IActionResult RedirectToLocalPage()
    {
        if (!string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }
}
