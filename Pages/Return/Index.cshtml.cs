using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;

namespace School_Library_Management.Pages.Return;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<ReturnRow> RecentReturns { get; private set; } = [];
    public int ReturnedToday { get; private set; }
    public int PendingReturns { get; private set; }
    public int LateReturns { get; private set; }

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        ReturnedToday = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate >= today && record.ReturnDate < tomorrow)
            .SumAsync(record => (int?)record.Quantity) ?? 0;

        PendingReturns = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate == null)
            .SumAsync(record => (int?)record.Quantity) ?? 0;

        LateReturns = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate != null && record.ReturnDate > record.DueDate)
            .SumAsync(record => (int?)record.Quantity) ?? 0;

        var recent = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate != null)
            .OrderByDescending(record => record.ReturnDate)
            .ThenByDescending(record => record.Id)
            .Take(6)
            .Select(record => new
            {
                record.Id,
                MemberName = record.Member.Name,
                BookTitle = record.Book.Title,
                ReturnDate = record.ReturnDate!.Value,
                record.DueDate,
                record.FineAmount,
                record.ReturnCondition
            })
            .ToListAsync();

        RecentReturns = recent.Select(record => new ReturnRow(
            FormatLoanId(record.Id),
            record.MemberName,
            record.BookTitle,
            record.ReturnDate,
            record.FineAmount,
            string.IsNullOrWhiteSpace(record.ReturnCondition)
                ? record.ReturnDate > record.DueDate ? "Late" : "Good"
                : record.ReturnCondition))
            .ToList();
    }

    public static string FormatLoanId(int id) => $"LOAN{id:00000}";

    public sealed record ReturnRow(
        string LoanId,
        string MemberName,
        string BookTitle,
        DateTime ReturnDate,
        decimal FineAmount,
        string Condition);
}
