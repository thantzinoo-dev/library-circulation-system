using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Return;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private static readonly string[] AllowedConditions = ["Good", "Late", "Damaged"];
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public ReturnInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<LoanOption> OpenLoans { get; private set; } = [];
    public IReadOnlyList<ReturnRow> RecentReturns { get; private set; } = [];
    public int ReturnedToday { get; private set; }
    public int PendingReturns { get; private set; }
    public int LateReturns { get; private set; }

    public async Task OnGetAsync()
    {
        Input.ReturnDate = DateTime.Today;
        Input.ReturnCondition = "Good";
        await LoadPageDataAsync(setDefaults: true);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.ReturnDate = Input.ReturnDate.Date;
        Input.ReturnCondition = Input.ReturnCondition?.Trim() ?? string.Empty;

        if (!AllowedConditions.Contains(Input.ReturnCondition, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.ReturnCondition", "Select a valid return condition.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var record = await _context.BorrowRecords
                .Include(r => r.Book)
                .SingleOrDefaultAsync(r => r.Id == Input.LoanId);

            if (record is null)
            {
                ModelState.AddModelError("Input.LoanId", "The selected loan does not exist.");
            }
            else if (record.ReturnDate is not null || record.Status == BorrowStatus.Returned)
            {
                ModelState.AddModelError("Input.LoanId", $"{FormatLoanId(record.Id)} has already been returned.");
            }
            else if (Input.ReturnDate < record.BorrowDate.Date)
            {
                ModelState.AddModelError("Input.ReturnDate", "Return date cannot be before the borrow date.");
            }
            else if (record.Book.AvailableCopies + record.Quantity > record.Book.TotalCopies)
            {
                ModelState.AddModelError(string.Empty, "Returning this loan would exceed the book's total copy count.");
            }

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                await LoadPageDataAsync();
                return Page();
            }

            record!.ReturnDate = Input.ReturnDate;
            record.Status = BorrowStatus.Returned;
            record.FineAmount = Input.FineAmount;
            record.ReturnCondition = Input.ReturnCondition;
            record.Book.AvailableCopies += record.Quantity;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            StatusMessage = $"{FormatLoanId(record.Id)} returned successfully.";
            return RedirectToPage();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, "The return could not be saved. Please refresh and try again.");
            await LoadPageDataAsync();
            return Page();
        }
    }

    private async Task LoadPageDataAsync(bool setDefaults = false)
    {
        var openLoans = await _context.BorrowRecords
            .AsNoTracking()
            .Where(r => r.ReturnDate == null && r.Status != BorrowStatus.Returned)
            .Include(r => r.Member)
            .Include(r => r.Book)
            .OrderByDescending(r => r.BorrowDate)
            .ThenByDescending(r => r.Id)
            .Select(r => new
            {
                r.Id,
                MemberName = r.Member.Name,
                r.Member.StudentId,
                BookTitle = r.Book.Title,
                Isbn = r.Book.ISBN,
                r.BorrowDate,
                r.DueDate,
                r.Quantity
            })
            .ToListAsync();

        OpenLoans = openLoans.Select(r => new LoanOption(
            r.Id,
            FormatLoanId(r.Id),
            r.MemberName,
            r.StudentId,
            r.BookTitle,
            r.Isbn ?? "—",
            r.BorrowDate,
            r.DueDate,
            r.Quantity)).ToList();

        if (setDefaults)
        {
            Input.LoanId = OpenLoans.FirstOrDefault()?.Id ?? 0;
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        ReturnedToday = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate >= today && r.ReturnDate < tomorrow)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        PendingReturns = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        LateReturns = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate != null && r.ReturnDate > r.DueDate)
            .SumAsync(r => (int?)r.Quantity) ?? 0;

        var recent = await _context.BorrowRecords
            .AsNoTracking()
            .Where(r => r.ReturnDate != null)
            .Include(r => r.Member)
            .Include(r => r.Book)
            .OrderByDescending(r => r.ReturnDate)
            .ThenByDescending(r => r.Id)
            .Take(6)
            .Select(r => new
            {
                r.Id,
                MemberName = r.Member.Name,
                BookTitle = r.Book.Title,
                ReturnDate = r.ReturnDate!.Value,
                r.DueDate,
                r.FineAmount,
                r.ReturnCondition
            })
            .ToListAsync();

        RecentReturns = recent.Select(r => new ReturnRow(
            FormatLoanId(r.Id),
            r.MemberName,
            r.BookTitle,
            r.ReturnDate,
            r.FineAmount,
            string.IsNullOrWhiteSpace(r.ReturnCondition) ? (r.ReturnDate > r.DueDate ? "Late" : "Good") : r.ReturnCondition))
            .ToList();
    }

    public static string FormatLoanId(int id) => $"LOAN{id:00000}";

    public sealed class ReturnInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select an open loan.")]
        public int LoanId { get; set; }

        [DataType(DataType.Date)]
        public DateTime ReturnDate { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999.99", ErrorMessage = "Fine amount cannot be negative.")]
        public decimal FineAmount { get; set; }

        [Required]
        [StringLength(50)]
        public string ReturnCondition { get; set; } = "Good";
    }

    public sealed record LoanOption(int Id, string LoanId, string MemberName, string StudentId, string BookTitle, string Isbn, DateTime BorrowDate, DateTime DueDate, int Quantity);
    public sealed record ReturnRow(string LoanId, string MemberName, string BookTitle, DateTime ReturnDate, decimal FineAmount, string Condition);
}
