using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;

namespace School_Library_Management.Pages.History;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    public const int PageSize = 8;
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Period { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public IReadOnlyList<HistoryRow> Records { get; private set; } = [];
    public int TotalRecords { get; private set; }
    public int BorrowedRecords { get; private set; }
    public int ReturnedRecords { get; private set; }
    public int OverdueRecords { get; private set; }
    public int FilteredCount { get; private set; }
    public int TotalPages { get; private set; }
    public int FirstRecord => FilteredCount == 0 ? 0 : ((PageIndex - 1) * PageSize) + 1;
    public int LastRecord => Math.Min(PageIndex * PageSize, FilteredCount);

    public async Task OnGetAsync()
    {
        Search = Search?.Trim();
        Status = string.IsNullOrWhiteSpace(Status) ? "All" : Status;
        Period = string.IsNullOrWhiteSpace(Period) ? "ThisMonth" : Period;

        var today = DateTime.Today;
        var baseQuery = _context.BorrowRecords.AsNoTracking();

        TotalRecords = await baseQuery.CountAsync();
        ReturnedRecords = await baseQuery.CountAsync(r => r.ReturnDate != null);
        OverdueRecords = await baseQuery.CountAsync(r => r.ReturnDate == null && r.DueDate < today);
        BorrowedRecords = await baseQuery.CountAsync(r => r.ReturnDate == null && r.DueDate >= today);

        var query = baseQuery.AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var term = Search;
            var numericLoan = term.StartsWith("LOAN", StringComparison.OrdinalIgnoreCase)
                ? term[4..]
                : term;
            var hasLoanId = int.TryParse(numericLoan, out var loanId);

            query = query.Where(r =>
                r.Member.Name.Contains(term) ||
                r.Member.StudentId.Contains(term) ||
                r.Book.Title.Contains(term) ||
                (hasLoanId && r.Id == loanId));
        }

        query = Status switch
        {
            "Borrowed" => query.Where(r => r.ReturnDate == null && r.DueDate >= today),
            "Returned" => query.Where(r => r.ReturnDate != null),
            "Overdue" => query.Where(r => r.ReturnDate == null && r.DueDate < today),
            _ => query
        };

        var (start, end) = GetPeriodBounds(Period, today);
        if (start.HasValue)
        {
            query = query.Where(r => r.BorrowDate >= start.Value && r.BorrowDate < end!.Value);
        }

        FilteredCount = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(FilteredCount / (double)PageSize));
        PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        var rows = await query
            .OrderByDescending(r => r.BorrowDate)
            .ThenByDescending(r => r.Id)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .Select(r => new
            {
                r.Id,
                MemberName = r.Member.Name,
                BookTitle = r.Book.Title,
                r.BorrowDate,
                r.DueDate,
                r.ReturnDate,
                r.FineAmount
            })
            .ToListAsync();

        Records = rows.Select(r => new HistoryRow(
            $"LOAN{r.Id:00000}",
            r.MemberName,
            r.BookTitle,
            r.BorrowDate,
            r.DueDate,
            r.ReturnDate,
            r.ReturnDate is not null ? "Returned" : r.DueDate < today ? "Overdue" : "Borrowed",
            r.FineAmount)).ToList();
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

    public sealed record HistoryRow(string LoanId, string MemberName, string BookTitle, DateTime BorrowDate, DateTime DueDate, DateTime? ReturnDate, string Status, decimal FineAmount);
}
