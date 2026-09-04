using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Members;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public CreateModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Member Input { get; set; } = new()
    {
        MembershipType = MemberType.Student,
        IsActive = true
    };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NormalizeInput();
        ModelState.Clear();
        TryValidateModel(Input, nameof(Input));

        if (!MemberType.All.Contains(Input.MembershipType))
        {
            ModelState.AddModelError("Input.MembershipType", "Please select a valid membership type.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await _context.Members.AnyAsync(m => m.StudentId == Input.StudentId))
        {
            ModelState.AddModelError("Input.StudentId", "A member with this Student ID already exists.");
            return Page();
        }

        var member = new Member
        {
            StudentId = Input.StudentId,
            Name = Input.Name,
            MembershipType = Input.MembershipType,
            Department = Input.Department,
            Phone = Input.Phone,
            Email = Input.Email,
            IsActive = Input.IsActive
        };

        _context.Members.Add(member);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueStudentIdViolation(exception))
        {
            ModelState.AddModelError("Input.StudentId", "A member with this Student ID already exists.");
            return Page();
        }

        TempData["StatusMessage"] = $"Member “{member.Name}” ({member.StudentId}) was added.";
        return RedirectToPage("./Index");
    }

    private void NormalizeInput()
    {
        Input.StudentId = Input.StudentId?.Trim() ?? string.Empty;
        Input.Name = Input.Name?.Trim() ?? string.Empty;
        Input.MembershipType = Input.MembershipType?.Trim() ?? MemberType.Student;
        Input.Department = NormalizeOptional(Input.Department);
        Input.Phone = NormalizeOptional(Input.Phone);
        Input.Email = NormalizeOptional(Input.Email);
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool IsUniqueStudentIdViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
