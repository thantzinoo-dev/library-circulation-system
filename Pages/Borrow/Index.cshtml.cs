using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Borrow;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BorrowInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public IReadOnlyList<MemberOption> Members { get; private set; } = [];
    public IReadOnlyList<BookOption> Books { get; private set; } = [];
    public IReadOnlyList<LoanRow> RecentLoans { get; private set; } = [];
    public int ActiveLoans { get; private set; }
    public int DueToday { get; private set; }
    public int OverdueLoans { get; private set; }

    public async Task OnGetAsync()
    {
        Input.BorrowDate = DateTime.Today;
        Input.DueDate = DateTime.Today.AddDays(14);
        Input.Quantity = 1;
        await LoadPageDataAsync(setDefaults: true);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.BorrowDate = Input.BorrowDate.Date;
        Input.DueDate = Input.DueDate.Date;

        if (Input.DueDate < Input.BorrowDate)
        {
            ModelState.AddModelError("Input.DueDate", "Due date must be on or after the borrow date.");
        }

        if (!ModelState.IsValid)
        {
            await LoadPageDataAsync();
            return Page();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var member = await _context.Members
                .SingleOrDefaultAsync(m => m.Id == Input.MemberId && m.IsActive);
            var book = await _context.Books
                .SingleOrDefaultAsync(b => b.Id == Input.BookId);

            if (member is null)
            {
                ModelState.AddModelError("Input.MemberId", "Select an active library member.");
            }

            if (book is null)
            {
                ModelState.AddModelError("Input.BookId", "The selected book no longer exists.");
            }
            else if (Input.Quantity > book.AvailableCopies)
            {
                ModelState.AddModelError("Input.Quantity", $"Only {book.AvailableCopies} cop{(book.AvailableCopies == 1 ? "y is" : "ies are")} available.");
            }

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                await LoadPageDataAsync();
                return Page();
            }

            var record = new BorrowRecord
            {
                MemberId = member!.Id,
                BookId = book!.Id,
                BorrowDate = Input.BorrowDate,
                DueDate = Input.DueDate,
                Quantity = Input.Quantity,
                Notes = string.IsNullOrWhiteSpace(Input.Notes) ? null : Input.Notes.Trim(),
                Status = BorrowStatus.Borrowed,
                FineAmount = 0m
            };

            book.AvailableCopies -= Input.Quantity;
            _context.BorrowRecords.Add(record);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            StatusMessage = $"{FormatLoanId(record.Id)} created successfully.";
            return RedirectToPage();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            ModelState.AddModelError(string.Empty, "The borrowing record could not be saved. Please refresh and try again.");
            await LoadPageDataAsync();
            return Page();
        }
    }

    private async Task LoadPageDataAsync(bool setDefaults = false)
    {
        Members = await _context.Members
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .Select(m => new MemberOption(m.Id, m.Name, m.StudentId))
            .ToListAsync();

        Books = await _context.Books
            .AsNoTracking()
            .Where(b => b.AvailableCopies > 0)
            .OrderBy(b => b.Title)
            .Select(b => new BookOption(b.Id, b.Title, b.ISBN ?? "—", b.AvailableCopies))
            .ToListAsync();

        if (setDefaults)
        {
            Input.MemberId = Members.FirstOrDefault()?.Id ?? 0;
            Input.BookId = Books.FirstOrDefault()?.Id ?? 0;
        }

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        ActiveLoans = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        DueToday = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null && r.DueDate >= today && r.DueDate < tomorrow)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        OverdueLoans = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null && r.DueDate < today)
            .SumAsync(r => (int?)r.Quantity) ?? 0;

        var recent = await _context.BorrowRecords
            .AsNoTracking()
            .Include(r => r.Member)
            .Include(r => r.Book)
            .OrderByDescending(r => r.BorrowDate)
            .ThenByDescending(r => r.Id)
            .Take(6)
            .Select(r => new
            {
                r.Id,
                MemberName = r.Member.Name,
                BookTitle = r.Book.Title,
                r.BorrowDate,
                r.DueDate,
                r.ReturnDate
            })
            .ToListAsync();

        RecentLoans = recent.Select(r => new LoanRow(
            FormatLoanId(r.Id),
            r.MemberName,
            r.BookTitle,
            r.BorrowDate,
            r.DueDate,
            r.ReturnDate is not null ? BorrowStatus.Returned : r.DueDate < today ? BorrowStatus.Overdue : r.DueDate < today.AddDays(4) ? "Due Soon" : "Issued"))
            .ToList();
    }

    public static string FormatLoanId(int id) => $"LOAN{id:00000}";

    public sealed class BorrowInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Select a member.")]
        public int MemberId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Select a book.")]
        public int BookId { get; set; }

        [DataType(DataType.Date)]
        public DateTime BorrowDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime DueDate { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1.")]
        public int Quantity { get; set; } = 1;

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public sealed record MemberOption(int Id, string Name, string StudentId);
    public sealed record BookOption(int Id, string Title, string Isbn, int AvailableCopies);
    public sealed record LoanRow(string LoanId, string MemberName, string BookTitle, DateTime BorrowDate, DateTime DueDate, string Status);
}
