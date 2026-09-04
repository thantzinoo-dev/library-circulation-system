using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Books;

public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Book Input { get; set; } = new();

    public int BorrowedCopies { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var book = await _context.Books.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id);
        if (book is null)
        {
            return NotFound();
        }

        Input = book;
        BorrowedCopies = book.TotalCopies - book.AvailableCopies;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var book = await _context.Books.SingleOrDefaultAsync(item => item.Id == id);
        if (book is null)
        {
            return NotFound();
        }

        BorrowedCopies = book.TotalCopies - book.AvailableCopies;
        NormalizeInput();
        ModelState.Clear();
        TryValidateModel(Input, nameof(Input));

        if (Input.TotalCopies < BorrowedCopies)
        {
            ModelState.AddModelError("Input.TotalCopies", $"Total copies cannot be less than the {BorrowedCopies} currently borrowed.");
        }

        if (!ModelState.IsValid)
        {
            Input.Id = id;
            return Page();
        }

        if (Input.ISBN is not null && await _context.Books.AnyAsync(item => item.Id != id && item.ISBN == Input.ISBN))
        {
            ModelState.AddModelError("Input.ISBN", "A book with this ISBN already exists.");
            Input.Id = id;
            return Page();
        }

        book.ISBN = Input.ISBN;
        book.Title = Input.Title;
        book.Author = Input.Author;
        book.Category = Input.Category;
        book.Language = Input.Language;
        book.PublishedYear = Input.PublishedYear;
        book.TotalCopies = Input.TotalCopies;
        book.AvailableCopies = Input.TotalCopies - BorrowedCopies;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueIsbnViolation(exception))
        {
            ModelState.AddModelError("Input.ISBN", "A book with this ISBN already exists.");
            Input.Id = id;
            return Page();
        }

        TempData["StatusMessage"] = $"“{book.Title}” was updated.";
        return RedirectToPage("./Index");
    }

    private void NormalizeInput()
    {
        Input.ISBN = NormalizeIsbn(Input.ISBN);
        Input.Title = Input.Title?.Trim() ?? string.Empty;
        Input.Author = Input.Author?.Trim() ?? string.Empty;
        Input.Category = NormalizeOptional(Input.Category);
        Input.Language = NormalizeOptional(Input.Language);
    }

    private static string? NormalizeIsbn(string? value)
    {
        var normalized = value?.Trim().Replace("-", string.Empty).Replace(" ", string.Empty).ToUpperInvariant();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }

    private static bool IsUniqueIsbnViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };
}
