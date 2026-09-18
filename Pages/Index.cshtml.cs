using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;

namespace School_Library_Management.Pages;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public const int PageSize = 12;
    public IReadOnlyList<PublicBook> Books { get; private set; } = [];
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public int TotalTitles { get; private set; }
    public int AvailableTitles { get; private set; }
    public int FilteredTitles { get; private set; }
    public int TotalPages { get; private set; }
    public int StartItem => FilteredTitles == 0 ? 0 : ((PageIndex - 1) * PageSize) + 1;
    public int EndItem => Math.Min(PageIndex * PageSize, FilteredTitles);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public async Task OnGetAsync()
    {
        Search = Search?.Trim();
        if (Search?.Length > 100)
        {
            Search = Search[..100];
        }

        var books = _context.Books.AsNoTracking();
        TotalTitles = await books.CountAsync();
        AvailableTitles = await books.CountAsync(book => book.AvailableCopies > 0);
        Categories = await books
            .Select(book => book.Category ?? "Uncategorized")
            .Distinct()
            .OrderBy(category => category)
            .ToListAsync();

        Category = Category?.Trim();
        Category = Categories.FirstOrDefault(category =>
            category.Equals(Category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search;
            books = books.Where(book =>
                book.Title.Contains(term) ||
                book.Author.Contains(term) ||
                (book.ISBN != null && book.ISBN.Contains(term)) ||
                (book.Category != null && book.Category.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(Category))
        {
            var selectedCategory = Category;
            books = selectedCategory == "Uncategorized"
                ? books.Where(book => book.Category == null)
                : books.Where(book => book.Category == selectedCategory);
        }

        FilteredTitles = await books.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredTitles / (double)PageSize));
        PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        Books = await books
            .OrderByDescending(book => book.AvailableCopies > 0)
            .ThenBy(book => book.Title)
            .ThenBy(book => book.Author)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .Select(book => new PublicBook(
                book.Id,
                book.Title,
                book.Author,
                book.Category ?? "Uncategorized",
                book.PublishedYear,
                book.AvailableCopies,
                book.CoverImagePath,
                book.ISBN))
            .ToListAsync();
    }

    public sealed record PublicBook(
        int Id,
        string Title,
        string Author,
        string Category,
        int PublishedYear,
        int AvailableCopies,
        string? CoverImagePath,
        string? Isbn)
    {
        public string Initials => string.Concat(Title
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));
    }
}
