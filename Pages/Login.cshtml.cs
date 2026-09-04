using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Library_Management.Pages;

/// <summary>
/// Login page. UI only for the MVP: the POST handler performs no authentication
/// yet, it just round-trips the submitted values so the real provider can be
/// wired in later without changing the form contract.
/// </summary>
public class LoginModel : PageModel
{
    [BindProperty]
    public string? UsernameOrEmail { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public bool RememberMe { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        // Authentication is intentionally not implemented at this stage.
        // Keep the field names stable so ASP.NET Core auth can be added later.
        if (string.IsNullOrWhiteSpace(UsernameOrEmail))
        {
            ModelState.AddModelError(nameof(UsernameOrEmail), "Enter your email or username.");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ModelState.AddModelError(nameof(Password), "Enter your password.");
        }

        // Never echo the submitted password back into the rendered page.
        Password = null;

        return Page();
    }
}
