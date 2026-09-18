using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Public;

public class MyBorrowingsModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty(SupportsGet = true)]
    public string? StudentId { get; set; }

    public bool SearchPerformed { get; private set; }
    public bool MemberFound { get; private set; }
    public IReadOnlyList<BorrowingItem> Borrowings { get; private set; } = [];

    public async Task OnGetAsync()
    {
        StudentId = StudentId?.Trim();
        if (string.IsNullOrWhiteSpace(StudentId))
        {
            return;
        }

        SearchPerformed = true;
        if (StudentId.Length > 30)
        {
            return;
        }

        var member = await _context.Members
            .AsNoTracking()
            .Where(candidate => candidate.StudentId == StudentId)
            .Select(candidate => new { candidate.Id })
            .SingleOrDefaultAsync();

        if (member is null)
        {
            return;
        }

        MemberFound = true;
        var today = DateTime.Today;
        var records = await _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.MemberId == member.Id)
            .OrderByDescending(record => record.BorrowDate)
            .ThenByDescending(record => record.Id)
            .Select(record => new
            {
                record.Id,
                BookTitle = record.Book.Title,
                record.Book.CoverImagePath,
                record.BorrowDate,
                record.DueDate,
                record.ReturnDate,
                record.FineAmount
            })
            .ToListAsync();

        Borrowings = records.Select(record =>
        {
            var status = record.ReturnDate is not null
                ? BorrowStatus.Returned
                : record.DueDate < today ? BorrowStatus.Overdue : BorrowStatus.Borrowed;
            var dayDifference = (record.DueDate.Date - today).Days;

            return new BorrowingItem(
                BorrowModel.FormatLoanId(record.Id),
                record.BookTitle,
                record.CoverImagePath,
                record.BorrowDate,
                record.DueDate,
                record.ReturnDate,
                status,
                dayDifference,
                record.FineAmount);
        }).ToList();
    }

    public sealed record BorrowingItem(
        string LoanId,
        string BookTitle,
        string? CoverImagePath,
        DateTime BorrowDate,
        DateTime DueDate,
        DateTime? ReturnDate,
        string Status,
        int DayDifference,
        decimal FineAmount);
}
