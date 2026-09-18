using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;
using School_Library_Management.Services;

namespace School_Library_Management.Pages.Borrow;

public class CreateModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public BorrowInput Input { get; set; } = new();

    [BindProperty(Name = "memberId", SupportsGet = true)]
    public int? RequestedMemberId { get; set; }

    public MemberOption? SelectedMember { get; private set; }
    public BookOption? SelectedBook { get; private set; }
    public IReadOnlyList<BookOption> AvailableBooks { get; private set; } = [];
    public int MaximumActiveLoans => BorrowingPolicy.MaximumActiveLoansPerMember;

    public async Task OnGetAsync()
    {
        Input.MemberId = RequestedMemberId.GetValueOrDefault();
        Input.BorrowDate = DateTime.Today;
        Input.DueDate = DateTime.Today.AddDays(14);
        Input.Quantity = 1;
        await LoadPageDataAsync();
    }

    public async Task<IActionResult> OnGetSearchMembersAsync(string? query)
    {
        var search = query?.Trim();
        if (string.IsNullOrWhiteSpace(search) || search.Length < 2)
        {
            return new JsonResult(Array.Empty<MemberOption>());
        }

        search = search[..Math.Min(search.Length, 100)];

        var members = await MemberQuery()
            .Where(member =>
                member.StudentId.Contains(search) ||
                member.Name.Contains(search) ||
                (member.Phone != null && member.Phone.Contains(search)) ||
                (member.Email != null && member.Email.Contains(search)))
            .OrderBy(member => member.StudentId == search ? 0 : member.Name.StartsWith(search) ? 1 : 2)
            .ThenBy(member => member.Name)
            .ThenBy(member => member.StudentId)
            .Take(12)
            .Select(member => new MemberOption(
                member.Id,
                member.StudentId,
                member.Name,
                member.MembershipType,
                member.Department,
                member.Phone,
                member.Email,
                member.BorrowRecords.Where(record => record.ReturnDate == null).Sum(record => (int?)record.Quantity) ?? 0,
                member.BorrowRecords.Any(record => record.ReturnDate == null && record.DueDate < DateTime.Today)))
            .ToListAsync();

        return new JsonResult(members);
    }

    public async Task<IActionResult> OnGetSearchBooksAsync(string? query)
    {
        var search = query?.Trim();
        var booksQuery = _context.Books
            .AsNoTracking()
            .Where(book => book.AvailableCopies > 0);

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search[..Math.Min(search.Length, 100)];
            booksQuery = booksQuery.Where(book =>
                book.Title.Contains(search) ||
                book.Author.Contains(search) ||
                (book.ISBN != null && book.ISBN.Contains(search)) ||
                (book.Category != null && book.Category.Contains(search)));
        }

        var books = await booksQuery
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Author)
            .Take(18)
            .Select(book => new BookOption(
                book.Id,
                book.Title,
                book.Author,
                book.ISBN ?? "—",
                book.PublishedYear,
                book.Category ?? "Uncategorized",
                book.AvailableCopies,
                book.CoverImagePath))
            .ToListAsync();

        return new JsonResult(books);
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
                .SingleOrDefaultAsync(candidate => candidate.Id == Input.MemberId && candidate.IsActive);
            var book = await _context.Books
                .SingleOrDefaultAsync(candidate => candidate.Id == Input.BookId);

            if (member is null)
            {
                ModelState.AddModelError("Input.MemberId", "Select an active library member.");
            }
            else
            {
                var activeLoanQuantity = await _context.BorrowRecords
                    .Where(record => record.MemberId == member.Id && record.ReturnDate == null)
                    .SumAsync(record => (int?)record.Quantity) ?? 0;
                var remainingLoanCapacity = BorrowingPolicy.MaximumActiveLoansPerMember - activeLoanQuantity;

                if (remainingLoanCapacity <= 0)
                {
                    ModelState.AddModelError(
                        "Input.MemberId",
                        $"This member already has {activeLoanQuantity} active loans. Return a book before borrowing another.");
                }
                else if (Input.Quantity > remainingLoanCapacity)
                {
                    ModelState.AddModelError(
                        "Input.Quantity",
                        $"This member can borrow only {remainingLoanCapacity} more book{(remainingLoanCapacity == 1 ? string.Empty : "s")}. The active-loan limit is {BorrowingPolicy.MaximumActiveLoansPerMember}.");
                }
            }

            if (book is null)
            {
                ModelState.AddModelError("Input.BookId", "The selected book no longer exists.");
            }
            else if (Input.Quantity > book.AvailableCopies)
            {
                ModelState.AddModelError(
                    "Input.Quantity",
                    $"Only {book.AvailableCopies} cop{(book.AvailableCopies == 1 ? "y is" : "ies are")} available.");
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

            TempData["StatusMessage"] = $"{IndexModel.FormatLoanId(record.Id)} created successfully for {member.Name}.";
            return RedirectToPage("./Index");
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

    private IQueryable<Member> MemberQuery() => _context.Members
        .AsNoTracking()
        .Where(member => member.IsActive);

    private async Task LoadPageDataAsync()
    {
        var today = DateTime.Today;

        if (Input.MemberId > 0)
        {
            SelectedMember = await MemberQuery()
                .Where(member => member.Id == Input.MemberId)
                .Select(member => new MemberOption(
                    member.Id,
                    member.StudentId,
                    member.Name,
                    member.MembershipType,
                    member.Department,
                    member.Phone,
                    member.Email,
                    member.BorrowRecords.Where(record => record.ReturnDate == null).Sum(record => (int?)record.Quantity) ?? 0,
                    member.BorrowRecords.Any(record => record.ReturnDate == null && record.DueDate < today)))
                .SingleOrDefaultAsync();
        }

        AvailableBooks = await _context.Books
            .AsNoTracking()
            .Where(book => book.AvailableCopies > 0)
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Author)
            .Take(18)
            .Select(book => new BookOption(
                book.Id,
                book.Title,
                book.Author,
                book.ISBN ?? "—",
                book.PublishedYear,
                book.Category ?? "Uncategorized",
                book.AvailableCopies,
                book.CoverImagePath))
            .ToListAsync();

        if (Input.BookId > 0)
        {
            SelectedBook = await _context.Books
                .AsNoTracking()
                .Where(book => book.Id == Input.BookId)
                .Select(book => new BookOption(
                    book.Id,
                    book.Title,
                    book.Author,
                    book.ISBN ?? "—",
                    book.PublishedYear,
                    book.Category ?? "Uncategorized",
                    book.AvailableCopies,
                    book.CoverImagePath))
                .SingleOrDefaultAsync();
        }
    }

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

    public sealed record MemberOption(
        int Id,
        string MemberCode,
        string Name,
        string MembershipType,
        string? Department,
        string? Phone,
        string? Email,
        int ActiveLoans,
        bool HasOverdueLoans);

    public sealed record BookOption(
        int Id,
        string Title,
        string Author,
        string Isbn,
        int PublishedYear,
        string Category,
        int AvailableCopies,
        string? CoverImagePath);
}
