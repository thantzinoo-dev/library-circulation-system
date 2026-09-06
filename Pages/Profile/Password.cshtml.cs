using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;
using School_Library_Management.Services;

namespace School_Library_Management.Pages.Profile;

[Authorize]
public class PasswordModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;
    private readonly PasswordHasher<AdminAccount> _passwordHasher = new();

    [BindProperty]
    public PasswordInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public bool FirstLogin { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var accountId = GetAccountId();
        if (accountId is null || !await _context.AdminAccounts.AsNoTracking().AnyAsync(item => item.Id == accountId))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }

        FirstLogin = FirstLogin || User.HasClaim(AdminAuthentication.MustChangePasswordClaim, "true");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!MeetsPasswordRequirements(Input.NewPassword))
        {
            ModelState.AddModelError("Input.NewPassword", "Use at least 10 characters with uppercase, lowercase, number, and symbol.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var accountId = GetAccountId();
        var account = accountId is null
            ? null
            : await _context.AdminAccounts.SingleOrDefaultAsync(item => item.Id == accountId);
        if (account is null || !account.IsActive)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Login");
        }

        var currentVerification = _passwordHasher.VerifyHashedPassword(account, account.PasswordHash, Input.CurrentPassword);
        if (currentVerification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("Input.CurrentPassword", "The current password is incorrect.");
            return Page();
        }

        if (_passwordHasher.VerifyHashedPassword(account, account.PasswordHash, Input.NewPassword) != PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("Input.NewPassword", "Choose a password different from the current password.");
            return Page();
        }

        account.PasswordHash = _passwordHasher.HashPassword(account, Input.NewPassword);
        account.MustChangePassword = false;
        account.FailedLoginAttempts = 0;
        account.LockoutEndUtc = null;
        account.UpdatedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var currentTicket = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            AdminAuthentication.CreatePrincipal(account),
            currentTicket.Properties);

        TempData["StatusMessage"] = "Password changed successfully.";
        return RedirectToPage("/Profile/Index");
    }

    private int? GetAccountId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var id) ? id : null;
    }

    private static bool MeetsPasswordRequirements(string password) =>
        password.Length >= 10 &&
        password.Any(char.IsUpper) &&
        password.Any(char.IsLower) &&
        password.Any(char.IsDigit) &&
        password.Any(character => !char.IsLetterOrDigit(character));

    public sealed class PasswordInput
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(10)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "The password confirmation does not match.")]
        [Display(Name = "Confirm new password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
