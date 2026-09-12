using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Borrow;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public const int PageSize = 10;
    private readonly ApplicationDbContext _context = context;

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<LoanRow> RecentLoans { get; private set; } = [];
    public int ActiveLoans { get; private set; }
    public int DueToday { get; private set; }
    public int OverdueLoans { get; private set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public int TotalItems { get; private set; }
    public int TotalPages { get; private set; }
    public int StartItem => TotalItems == 0 ? 0 : ((PageIndex - 1) * PageSize) + 1;
    public int EndItem => Math.Min(PageIndex * PageSize, TotalItems);
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        ActiveLoans = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate == null)
            .SumAsync(record => (int?)record.Quantity) ?? 0;

        DueToday = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate == null && record.DueDate >= today && record.DueDate < tomorrow)
            .SumAsync(record => (int?)record.Quantity) ?? 0;

        OverdueLoans = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate == null && record.DueDate < today)
            .SumAsync(record => (int?)record.Quantity) ?? 0;

        var recordsQuery = _context.BorrowRecords.AsNoTracking();
        TotalItems = await recordsQuery.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
        PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        var recent = await recordsQuery
            .AsNoTracking()
            .OrderByDescending(record => record.BorrowDate)
            .ThenByDescending(record => record.Id)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .Select(record => new
            {
                record.Id,
                MemberName = record.Member.Name,
                BookTitle = record.Book.Title,
                record.BorrowDate,
                record.DueDate,
                record.ReturnDate
            })
            .ToListAsync();

        RecentLoans = recent.Select(record => new LoanRow(
            FormatLoanId(record.Id),
            record.MemberName,
            record.BookTitle,
            record.BorrowDate,
            record.DueDate,
            record.ReturnDate is not null
                ? BorrowStatus.Returned
                : record.DueDate < today
                    ? BorrowStatus.Overdue
                    : record.DueDate < today.AddDays(4)
                        ? "Due Soon"
                        : "Issued"))
            .ToList();
    }

    public static string FormatLoanId(int id) => $"LOAN{id:00000}";

    public sealed record LoanRow(
        string LoanId,
        string MemberName,
        string BookTitle,
        DateTime BorrowDate,
        DateTime DueDate,
        string Status);
}
