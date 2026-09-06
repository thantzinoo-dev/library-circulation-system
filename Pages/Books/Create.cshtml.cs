using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;
using School_Library_Management.Services;

namespace School_Library_Management.Pages.Books;

public class CreateModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public CreateModel(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [BindProperty]
    public Book Input { get; set; } = new();

    [BindProperty]
    public IFormFile? CoverImage { get; set; }

    public void OnGet()
    {
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

        if (Input.ISBN is not null && await _context.Books.AnyAsync(book => book.ISBN == Input.ISBN))
        {
            ModelState.AddModelError("Input.ISBN", "A book with this ISBN already exists.");
            return Page();
        }

        string? coverImagePath = null;
        if (CoverImage is not null)
        {
            try
            {
                coverImagePath = await BookCoverStorage.SaveAsync(CoverImage, _environment, HttpContext.RequestAborted);
            }
            catch (ValidationException exception)
            {
                ModelState.AddModelError(nameof(CoverImage), exception.Message);
                return Page();
            }
        }

        var book = new Book
        {
            ISBN = Input.ISBN,
            Title = Input.Title,
            Author = Input.Author,
            Category = Input.Category,
            Language = Input.Language,
            PublishedYear = Input.PublishedYear,
            TotalCopies = Input.TotalCopies,
            AvailableCopies = Input.TotalCopies,
            CoverImagePath = coverImagePath
        };

        _context.Books.Add(book);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException exception) when (IsUniqueIsbnViolation(exception))
        {
            BookCoverStorage.DeleteUploadedCover(coverImagePath, _environment);
            ModelState.AddModelError("Input.ISBN", "A book with this ISBN already exists.");
            return Page();
        }

        TempData["StatusMessage"] = $"“{book.Title}” was added.";
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
