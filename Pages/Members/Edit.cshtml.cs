using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Members;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Member Input { get; set; } = new();

    public DateTime JoinDate { get; private set; }
    public int TotalBorrows { get; private set; }
    public int ActiveBorrows { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var member = await _context.Members
            .AsNoTracking()
            .Include(m => m.BorrowRecords)
            .SingleOrDefaultAsync(m => m.Id == id);

        if (member is null)
        {
            return NotFound();
        }

        Input = member;
        JoinDate = member.CreatedAt;
        TotalBorrows = member.BorrowRecords.Count;
        ActiveBorrows = member.BorrowRecords.Count(r => r.Status != BorrowStatus.Returned);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var member = await _context.Members
            .Include(m => m.BorrowRecords)
            .SingleOrDefaultAsync(m => m.Id == id);

        if (member is null)
        {
            return NotFound();
        }

        JoinDate = member.CreatedAt;
        TotalBorrows = member.BorrowRecords.Count;
        ActiveBorrows = member.BorrowRecords.Count(r => r.Status != BorrowStatus.Returned);

        NormalizeInput();
        ModelState.Clear();
        TryValidateModel(Input, nameof(Input));

        if (!MemberType.All.Contains(Input.MembershipType))
        {
            ModelState.AddModelError("Input.MembershipType", "Please select a valid membership type.");
        }

        if (!ModelState.IsValid)
        {
            Input.Id = id;
            return Page();
        }

        if (await _context.Members.AnyAsync(m => m.Id != id && m.StudentId == Input.StudentId))
        {
            ModelState.AddModelError("Input.StudentId", "Another member with this Student ID already exists.");
            Input.Id = id;
            return Page();
        }

        member.StudentId = Input.StudentId;
        member.Name = Input.Name;
        member.MembershipType = Input.MembershipType;
        member.Department = Input.Department;
        member.Phone = Input.Phone;
        member.Email = Input.Email;
        member.IsActive = Input.IsActive;
        // CreatedAt is intentionally preserved and not modified

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueStudentIdViolation(exception))
        {
            ModelState.AddModelError("Input.StudentId", "Another member with this Student ID already exists.");
            Input.Id = id;
            return Page();
        }

        TempData["StatusMessage"] = $"Member “{member.Name}” was updated.";
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
