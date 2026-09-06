using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Profile;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public string Initials
    {
        get
        {
            var initials = string.Concat((Input.FullName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(2)
                .Select(part => char.ToUpperInvariant(part[0])));
            return string.IsNullOrWhiteSpace(initials) ? "A" : initials;
        }
    }

    public async Task OnGetAsync()
    {
        var profile = await _context.AdminProfiles.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync();
        if (profile is null)
        {
            return;
        }

        Input = new ProfileInput
        {
            FullName = profile.FullName,
            Role = profile.Role,
            Email = profile.Email,
            Phone = profile.Phone,
            Department = profile.Department,
            Bio = profile.Bio
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NormalizeInput();
        ModelState.Clear();
        TryValidateModel(Input, nameof(Input));
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var profile = await _context.AdminProfiles.OrderBy(item => item.Id).FirstOrDefaultAsync();
        if (profile is null)
        {
            profile = new AdminProfile();
            _context.AdminProfiles.Add(profile);
        }

        profile.FullName = Input.FullName;
        profile.Role = Input.Role;
        profile.Email = Input.Email;
        profile.Phone = Input.Phone;
        profile.Department = Input.Department;
        profile.Bio = Input.Bio;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        StatusMessage = "Profile updated successfully.";
        return RedirectToPage();
    }

    private void NormalizeInput()
    {
        Input.FullName = Input.FullName?.Trim() ?? string.Empty;
        Input.Role = Input.Role?.Trim() ?? string.Empty;
        Input.Email = NormalizeOptional(Input.Email);
        Input.Phone = NormalizeOptional(Input.Phone);
        Input.Department = NormalizeOptional(Input.Department);
        Input.Bio = NormalizeOptional(Input.Bio);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    public sealed class ProfileInput
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full name")]
        public string FullName { get; set; } = "Admin User";

        [Required(ErrorMessage = "Role is required.")]
        [StringLength(60)]
        public string Role { get; set; } = "Administrator";

        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; set; }

        [Phone]
        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        [StringLength(500)]
        public string? Bio { get; set; }
    }
}
