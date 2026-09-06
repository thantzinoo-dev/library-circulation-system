using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Books;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public const int PageSize = 10;

    public IReadOnlyList<Book> Books { get; private set; } = Array.Empty<Book>();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<string> AvailableTypes { get; private set; } = Array.Empty<string>();

    public int TotalItems { get; private set; }
    public int TotalPages { get; private set; }
    public int StartItem { get; private set; }
    public int EndItem { get; private set; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Load all available book categories/types for the filter dropdown
        AvailableTypes = await _context.Books
            .Where(b => !string.IsNullOrEmpty(b.Category))
            .Select(b => b.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        IQueryable<Book> query = _context.Books.AsNoTracking();

        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        if (Search is not null)
        {
            var isbnSearch = Search.Replace("-", string.Empty).Replace(" ", string.Empty);
            if (isbnSearch.Length == 0)
            {
                isbnSearch = Search;
            }

            query = query.Where(book =>
                (book.ISBN != null && (book.ISBN.Contains(Search) || book.ISBN.Contains(isbnSearch))) ||
                book.Title.Contains(Search) ||
                book.Author.Contains(Search) ||
                (book.Category != null && book.Category.Contains(Search)) ||
                (book.Language != null && book.Language.Contains(Search)));
        }

        if (!string.IsNullOrWhiteSpace(Status) && !Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Status.Equals("Available", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.AvailableCopies > 0);
            }
            else if (Status.Equals("Unavailable", StringComparison.OrdinalIgnoreCase) ||
                     Status.Equals("OutOfStock", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.AvailableCopies == 0);
            }
            else if (Status.Equals("Borrowed", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(b => b.AvailableCopies < b.TotalCopies);
            }
        }

        var effectiveType = !string.IsNullOrWhiteSpace(Type) ? Type : Category;
        if (!string.IsNullOrWhiteSpace(effectiveType) && !effectiveType.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            Type = effectiveType;
            query = query.Where(b => b.Category != null && b.Category == effectiveType);
        }

        TotalItems = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
        if (TotalPages < 1)
        {
            TotalPages = 1;
        }

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }
        else if (PageIndex > TotalPages)
        {
            PageIndex = TotalPages;
        }

        StartItem = TotalItems == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
        EndItem = Math.Min(PageIndex * PageSize, TotalItems);

        Books = await query
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Author)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id, string? search, string? status, string? type, int pageIndex = 1)
    {
        var book = await _context.Books.FindAsync(id);
        if (book is null)
        {
            ErrorMessage = "The book could not be found.";
            return RedirectToPage(new { Search = search, Status = status, Type = type, PageIndex = pageIndex });
        }

        if (await _context.BorrowRecords.AnyAsync(record => record.BookId == id))
        {
            ErrorMessage = $"“{book.Title}” cannot be deleted because it has borrowing history.";
            return RedirectToPage(new { Search = search, Status = status, Type = type, PageIndex = pageIndex });
        }

        _context.Books.Remove(book);

        try
        {
            await _context.SaveChangesAsync();
            StatusMessage = $"“{book.Title}” was deleted.";
        }
        catch (DbUpdateException)
        {
            ErrorMessage = $"“{book.Title}” could not be deleted because it is referenced by another record.";
        }

        return RedirectToPage(new { Search = search, Status = status, Type = type, PageIndex = pageIndex });
    }
}
