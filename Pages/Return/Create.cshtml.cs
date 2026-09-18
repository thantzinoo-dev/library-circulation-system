using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Return;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private static readonly string[] AllowedConditions = ["Good", "Late", "Damaged"];
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public ReturnInput Input { get; set; } = new();

    [BindProperty(Name = "loanId", SupportsGet = true)]
    public int? RequestedLoanId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DueStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? MembershipType { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public LoanOption? SelectedLoan { get; private set; }
    public IReadOnlyList<LoanOption> ActiveLoans { get; private set; } = [];
    public int TotalActiveLoans { get; private set; }
    public int TotalPages { get; private set; }
    public const int PageSize = 8;
    public int StartItem => TotalActiveLoans == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;
    public int EndItem => Math.Min(PageNumber * PageSize, TotalActiveLoans);

    public async Task OnGetAsync()
    {
        Input.LoanId = RequestedLoanId.GetValueOrDefault();
        Input.ReturnDate = DateTime.Today;
        Input.ReturnCondition = "Good";
        await LoadPageDataAsync();
    }

    public async Task<JsonResult> OnGetLoanPageAsync()
    {
        NormalizeFilters();
        await LoadActiveLoansAsync();

        return new JsonResult(new
        {
            loans = ActiveLoans.Select(loan => new
            {
                loan.Id,
                loan.LoanId,
                loan.MemberName,
                loan.MemberCode,
                loan.MembershipType,
                loan.BookTitle,
                loan.Isbn,
                loan.CoverImagePath,
                borrowDate = loan.BorrowDate.ToString("MMM dd, yyyy"),
                borrowDateIso = loan.BorrowDate.ToString("yyyy-MM-dd"),
                dueDate = loan.DueDate.ToString("MMM dd, yyyy"),
                loan.Quantity,
                loan.IsOverdue,
                loan.DaysOverdue
            }),
            totalActiveLoans = TotalActiveLoans,
            totalPages = TotalPages,
            pageNumber = PageNumber,
            pageSize = PageSize,
            startItem = StartItem,
            endItem = EndItem
        });
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
                .Include(candidate => candidate.Book)
                .Include(candidate => candidate.Member)
                .SingleOrDefaultAsync(candidate => candidate.Id == Input.LoanId);

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

            TempData["StatusMessage"] = $"{FormatLoanId(record.Id)} returned successfully for {record.Member.Name}.";
            return RedirectToPage("./Index");
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

    private async Task LoadPageDataAsync()
    {
        NormalizeFilters();

        await LoadActiveLoansAsync();
        await LoadSelectedLoanAsync();
    }

    private void NormalizeFilters()
    {
        Search = Search?.Trim();
        DueStatus = DueStatus is "Active" or "Overdue" ? DueStatus : "All";
        MembershipType = MemberType.All.Contains(MembershipType, StringComparer.OrdinalIgnoreCase)
            ? MembershipType
            : "All";
    }

    private async Task LoadActiveLoansAsync()
    {
        var query = _context.BorrowRecords
            .AsNoTracking()
            .Where(record => record.ReturnDate == null && record.Status != BorrowStatus.Returned);

        var today = DateTime.Today;
        var search = Search;
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search[..Math.Min(search.Length, 120)];
            var loanNumberText = search.StartsWith("LOAN", StringComparison.OrdinalIgnoreCase)
                ? search[4..]
                : search;
            var hasLoanNumber = int.TryParse(loanNumberText, out var loanNumber);

            query = query.Where(record =>
                (hasLoanNumber && record.Id == loanNumber) ||
                record.Member.StudentId.Contains(search) ||
                record.Member.Name.Contains(search) ||
                record.Book.Title.Contains(search) ||
                (record.Book.ISBN != null && record.Book.ISBN.Contains(search)));
        }

        query = DueStatus switch
        {
            "Active" => query.Where(record => record.DueDate >= today),
            "Overdue" => query.Where(record => record.DueDate < today),
            _ => query
        };

        if (MembershipType != "All")
        {
            query = query.Where(record => record.Member.MembershipType == MembershipType);
        }

        TotalActiveLoans = await query.CountAsync();
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalActiveLoans / (double)PageSize));
        PageNumber = Math.Clamp(PageNumber, 1, TotalPages);

        var rows = await query
            .OrderBy(record => record.DueDate)
            .ThenBy(record => record.Id)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .Select(record => new
            {
                record.Id,
                MemberName = record.Member.Name,
                MemberCode = record.Member.StudentId,
                record.Member.MembershipType,
                BookTitle = record.Book.Title,
                Isbn = record.Book.ISBN ?? "—",
                record.Book.CoverImagePath,
                record.BorrowDate,
                record.DueDate,
                record.Quantity
            })
            .ToListAsync();

        ActiveLoans = rows.Select(record => ToLoanOption(
            record.Id,
            record.MemberName,
            record.MemberCode,
            record.MembershipType,
            record.BookTitle,
            record.Isbn,
            record.CoverImagePath,
            record.BorrowDate,
            record.DueDate,
            record.Quantity,
            today)).ToList();
    }

    private async Task LoadSelectedLoanAsync()
    {
        if (Input.LoanId <= 0)
        {
            return;
        }

        var record = await _context.BorrowRecords
            .AsNoTracking()
            .Where(candidate =>
                candidate.Id == Input.LoanId &&
                candidate.ReturnDate == null &&
                candidate.Status != BorrowStatus.Returned)
            .Select(candidate => new
            {
                candidate.Id,
                MemberName = candidate.Member.Name,
                MemberCode = candidate.Member.StudentId,
                candidate.Member.MembershipType,
                BookTitle = candidate.Book.Title,
                Isbn = candidate.Book.ISBN ?? "—",
                candidate.Book.CoverImagePath,
                candidate.BorrowDate,
                candidate.DueDate,
                candidate.Quantity
            })
            .SingleOrDefaultAsync();

        if (record is null)
        {
            return;
        }

        SelectedLoan = ToLoanOption(
            record.Id,
            record.MemberName,
            record.MemberCode,
            record.MembershipType,
            record.BookTitle,
            record.Isbn,
            record.CoverImagePath,
            record.BorrowDate,
            record.DueDate,
            record.Quantity,
            DateTime.Today);
    }

    private static LoanOption ToLoanOption(
        int id,
        string memberName,
        string memberCode,
        string membershipType,
        string bookTitle,
        string isbn,
        string? coverImagePath,
        DateTime borrowDate,
        DateTime dueDate,
        int quantity,
        DateTime today) => new(
            id,
            FormatLoanId(id),
            memberName,
            memberCode,
            membershipType,
            bookTitle,
            isbn,
            coverImagePath,
            borrowDate,
            dueDate,
            quantity,
            dueDate < today,
            Math.Max(0, (today - dueDate.Date).Days));

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

    public sealed record LoanOption(
        int Id,
        string LoanId,
        string MemberName,
        string MemberCode,
        string MembershipType,
        string BookTitle,
        string Isbn,
        string? CoverImagePath,
        DateTime BorrowDate,
        DateTime DueDate,
        int Quantity,
        bool IsOverdue,
        int DaysOverdue);
}
