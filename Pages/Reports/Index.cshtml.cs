using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Reports;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private static readonly string[] CategoryColors = ["#1468E8", "#20B97A", "#F59E0B", "#7C3AED", "#22A7C7", "#94A3B8"];
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public string? ReportType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Period { get; set; }

    public int TotalBorrowings { get; private set; }
    public int ReturnedBooks { get; private set; }
    public int OverdueBooks { get; private set; }
    public decimal FineCollected { get; private set; }
    public int MonthlyChartMax { get; private set; } = 10;
    public IReadOnlyList<MonthlyBorrowing> MonthlyBorrowings { get; private set; } = [];
    public IReadOnlyList<CategoryShare> CategoryShares { get; private set; } = [];
    public IReadOnlyList<PopularBook> PopularBooks { get; private set; } = [];
    public bool ShowPdfExport { get; private set; } = true;
    public bool ShowExcelExport { get; private set; } = true;

    public async Task OnGetAsync()
    {
        var settings = await _context.LibrarySettings.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync();
        ReportType = string.IsNullOrWhiteSpace(ReportType) ? "All" : ReportType;
        Period = string.IsNullOrWhiteSpace(Period) ? settings?.DefaultReportPeriod ?? ReportPeriod.ThisMonth : Period;
        ShowPdfExport = settings?.ShowPdfExport ?? true;
        ShowExcelExport = settings?.ShowExcelExport ?? true;

        var today = DateTime.Today;
        var (start, end) = GetPeriodBounds(Period, today);
        var records = _context.BorrowRecords.AsNoTracking();

        var borrowedQuery = records.AsQueryable();
        if (start.HasValue)
        {
            borrowedQuery = borrowedQuery.Where(r => r.BorrowDate >= start.Value && r.BorrowDate < end!.Value);
        }

        TotalBorrowings = await borrowedQuery.SumAsync(r => (int?)r.Quantity) ?? 0;

        var returnedQuery = records.Where(r => r.ReturnDate != null);
        if (start.HasValue)
        {
            returnedQuery = returnedQuery.Where(r => r.ReturnDate >= start.Value && r.ReturnDate < end!.Value);
        }

        ReturnedBooks = await returnedQuery.SumAsync(r => (int?)r.Quantity) ?? 0;
        FineCollected = await returnedQuery.SumAsync(r => (decimal?)r.FineAmount) ?? 0m;

        var overdueQuery = records.Where(r => r.ReturnDate == null && r.DueDate < today);
        if (start.HasValue)
        {
            overdueQuery = overdueQuery.Where(r => r.BorrowDate >= start.Value && r.BorrowDate < end!.Value);
        }

        OverdueBooks = await overdueQuery.SumAsync(r => (int?)r.Quantity) ?? 0;

        await LoadMonthlyBorrowingsAsync(today);
        await LoadCategorySharesAsync();
        await LoadPopularBooksAsync(start, end);
    }

    private async Task LoadMonthlyBorrowingsAsync(DateTime today)
    {
        var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-5);
        var endMonth = firstMonth.AddMonths(6);
        var rows = await _context.BorrowRecords
            .AsNoTracking()
            .Where(r => r.BorrowDate >= firstMonth && r.BorrowDate < endMonth)
            .Select(r => new { r.BorrowDate, r.Quantity })
            .ToListAsync();

        MonthlyBorrowings = Enumerable.Range(0, 6)
            .Select(offset => firstMonth.AddMonths(offset))
            .Select(month => new MonthlyBorrowing(
                month.ToString("MMM"),
                rows.Where(r => r.BorrowDate.Year == month.Year && r.BorrowDate.Month == month.Month).Sum(r => r.Quantity)))
            .ToList();

        var maximum = MonthlyBorrowings.Max(m => m.Count);
        MonthlyChartMax = Math.Max(10, (int)Math.Ceiling(maximum / 10d) * 10);
    }

    private async Task LoadCategorySharesAsync()
    {
        var categories = await _context.Books
            .AsNoTracking()
            .GroupBy(b => b.Category ?? "Uncategorized")
            .Select(group => new { Label = group.Key, Copies = group.Sum(b => b.TotalCopies) })
            .OrderByDescending(group => group.Copies)
            .ThenBy(group => group.Label)
            .ToListAsync();

        var totalCopies = categories.Sum(category => category.Copies);
        if (totalCopies == 0)
        {
            CategoryShares = [];
            return;
        }

        var displayedCategories = categories.Take(5)
            .Select(category => new { category.Label, category.Copies })
            .ToList();
        var otherCopies = categories.Skip(5).Sum(category => category.Copies);
        if (otherCopies > 0)
        {
            displayedCategories.Add(new { Label = "Other", Copies = otherCopies });
        }

        var offset = 0d;
        CategoryShares = displayedCategories.Select((category, index) =>
        {
            var percentage = category.Copies * 100d / totalCopies;
            var item = new CategoryShare(category.Label, percentage, offset, CategoryColors[index % CategoryColors.Length]);
            offset += percentage;
            return item;
        }).ToList();
    }

    private async Task LoadPopularBooksAsync(DateTime? start, DateTime? end)
    {
        var rows = await _context.Books
            .AsNoTracking()
            .Select(book => new
            {
                book.Title,
                Category = book.Category ?? "Uncategorized",
                book.AvailableCopies,
                book.CoverImagePath,
                TotalBorrowed = book.BorrowRecords
                    .Where(record => !start.HasValue || (record.BorrowDate >= start.Value && record.BorrowDate < end!.Value))
                    .Sum(record => (int?)record.Quantity) ?? 0,
                FineGenerated = book.BorrowRecords
                    .Where(record => !start.HasValue || (record.ReturnDate >= start.Value && record.ReturnDate < end!.Value))
                    .Sum(record => (decimal?)record.FineAmount) ?? 0m
            })
            .Where(book => book.TotalBorrowed > 0)
            .OrderByDescending(book => book.TotalBorrowed)
            .ThenBy(book => book.Title)
            .Take(5)
            .ToListAsync();

        PopularBooks = rows.Select(book => new PopularBook(
            book.Title,
            book.Category,
            book.TotalBorrowed,
            book.AvailableCopies,
            book.FineGenerated,
            GetInitials(book.Title),
            book.CoverImagePath)).ToList();
    }

    private static (DateTime? Start, DateTime? End) GetPeriodBounds(string? period, DateTime today)
    {
        return period switch
        {
            "LastMonth" => (new DateTime(today.Year, today.Month, 1).AddMonths(-1), new DateTime(today.Year, today.Month, 1)),
            "ThisYear" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year + 1, 1, 1)),
            "All" => (null, null),
            _ => (new DateTime(today.Year, today.Month, 1), new DateTime(today.Year, today.Month, 1).AddMonths(1))
        };
    }

    private static string GetInitials(string title) => string.Concat(
        title.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0])));

    public sealed record MonthlyBorrowing(string Month, int Count);
    public sealed record CategoryShare(string Label, double Percentage, double Offset, string Color);
    public sealed record PopularBook(string Title, string Category, int TotalBorrowed, int AvailableCopies, decimal FineGenerated, string Initials, string? CoverImagePath);
}
