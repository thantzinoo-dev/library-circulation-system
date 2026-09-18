using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;
using School_Library_Management.Services;

namespace School_Library_Management.Pages.Public;

public class BorrowModel(ApplicationDbContext context) : PageModel
{
    private const string ConfirmationKey = "PublicBorrowConfirmation";
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public GuestBorrowInput Input { get; set; } = new();

    public PublicBookDetails? Book { get; private set; }
    public BorrowConfirmation? Confirmation { get; private set; }
    public string BorrowDateValue => DateTime.Today.ToString("yyyy-MM-dd");
    public string MinimumDueDate => DateTime.Today.ToString("yyyy-MM-dd");
    public string MaximumDueDate => DateTime.Today.AddDays(7).ToString("yyyy-MM-dd");

    public async Task<IActionResult> OnGetAsync(int bookId)
    {
        Book = await LoadBookAsync(bookId);
        if (Book is null)
        {
            return NotFound();
        }

        Input.MembershipType = MemberType.Student;
        Input.DueDate = DateTime.Today.AddDays(7);
        LoadConfirmation();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int bookId)
    {
        NormalizeInput();
        ModelState.Clear();
        TryValidateModel(Input, nameof(Input));

        var today = DateTime.Today;
        var dueDate = Input.DueDate?.Date;

        if (!MemberType.All.Contains(Input.MembershipType, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError("Input.MembershipType", "Select a valid membership type.");
        }

        if (dueDate.HasValue && dueDate.Value < today)
        {
            ModelState.AddModelError("Input.DueDate", "Due date cannot be before today's borrow date.");
        }
        else if (dueDate.HasValue && dueDate.Value > today.AddDays(7))
        {
            ModelState.AddModelError("Input.DueDate", "Due date must be within 7 days of today's borrow date.");
        }

        Book = await LoadBookAsync(bookId);
        if (Book is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            var book = await _context.Books.SingleOrDefaultAsync(candidate => candidate.Id == bookId);
            if (book is null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (book.AvailableCopies <= 0)
            {
                ModelState.AddModelError(string.Empty, "This book is no longer available. Please choose another title.");
                await transaction.RollbackAsync();
                Book = ToBookDetails(book);
                return Page();
            }

            var member = await _context.Members
                .SingleOrDefaultAsync(candidate => candidate.StudentId == Input.StudentId);

            if (member is not null && !member.IsActive)
            {
                ModelState.AddModelError("Input.StudentId", "This library membership is inactive. Please contact the library desk.");
                await transaction.RollbackAsync();
                return Page();
            }

            if (member is not null)
            {
                var activeLoanQuantity = await _context.BorrowRecords
                    .Where(record => record.MemberId == member.Id && record.ReturnDate == null)
                    .SumAsync(record => (int?)record.Quantity) ?? 0;

                if (activeLoanQuantity >= BorrowingPolicy.MaximumActiveLoansPerMember)
                {
                    ModelState.AddModelError(
                        "Input.StudentId",
                        $"This member already has {activeLoanQuantity} active loans. Return a book before borrowing another.");
                    await transaction.RollbackAsync();
                    return Page();
                }
            }

            if (member is null)
            {
                member = new Member
                {
                    StudentId = Input.StudentId,
                    Name = Input.Name,
                    MembershipType = MemberType.All.First(type =>
                        type.Equals(Input.MembershipType, StringComparison.OrdinalIgnoreCase)),
                    Department = Input.Department,
                    Phone = Input.Phone,
                    Email = Input.Email,
                    IsActive = true
                };
                _context.Members.Add(member);
            }

            var record = new BorrowRecord
            {
                Book = book,
                Member = member,
                BorrowDate = today,
                DueDate = dueDate!.Value,
                ReturnDate = null,
                Status = BorrowStatus.Borrowed,
                Quantity = 1,
                FineAmount = 0m
            };

            book.AvailableCopies -= 1;
            _context.BorrowRecords.Add(record);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            var confirmation = new BorrowConfirmation(
                FormatLoanId(record.Id),
                book.Title,
                member.Name,
                today,
                dueDate.Value,
                member.StudentId);

            TempData[ConfirmationKey] = JsonSerializer.Serialize(confirmation);
            TempData["StatusMessage"] = $"{book.Title} was borrowed successfully.";
            return RedirectToPage("/Public/Borrow", new { bookId = book.Id });
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();
            Book = await LoadBookAsync(bookId);

            var message = IsUniqueStudentIdViolation(exception)
                ? "That Student ID was registered by another request. Please submit the form again."
                : "The borrowing could not be completed. Please refresh and try again.";
            ModelState.AddModelError(string.Empty, message);
            return Page();
        }
    }

    private async Task<PublicBookDetails?> LoadBookAsync(int bookId) =>
        await _context.Books
            .AsNoTracking()
            .Where(book => book.Id == bookId)
            .Select(book => new PublicBookDetails(
                book.Id,
                book.Title,
                book.Author,
                book.Category ?? "Uncategorized",
                book.PublishedYear,
                book.AvailableCopies,
                book.CoverImagePath,
                book.ISBN))
            .SingleOrDefaultAsync();

    private void LoadConfirmation()
    {
        if (TempData[ConfirmationKey] is not string json)
        {
            return;
        }

        try
        {
            Confirmation = JsonSerializer.Deserialize<BorrowConfirmation>(json);
        }
        catch (JsonException)
        {
            Confirmation = null;
        }
    }

    private void NormalizeInput()
    {
        Input.Name = Input.Name?.Trim() ?? string.Empty;
        Input.StudentId = Input.StudentId?.Trim() ?? string.Empty;
        Input.MembershipType = Input.MembershipType?.Trim() ?? string.Empty;
        Input.Department = NormalizeOptional(Input.Department);
        Input.Phone = NormalizeOptional(Input.Phone);
        Input.Email = NormalizeOptional(Input.Email);
        Input.DueDate = Input.DueDate?.Date;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }

    private static PublicBookDetails ToBookDetails(Book book) => new(
        book.Id,
        book.Title,
        book.Author,
        book.Category ?? "Uncategorized",
        book.PublishedYear,
        book.AvailableCopies,
        book.CoverImagePath,
        book.ISBN);

    private static bool IsUniqueStudentIdViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 };

    public static string FormatLoanId(int id) => $"LOAN{id:00000}";

    public sealed class GuestBorrowInput
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Student ID is required.")]
        [StringLength(30)]
        [Display(Name = "Student ID")]
        public string StudentId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Membership type is required.")]
        [StringLength(20)]
        [Display(Name = "Membership Type")]
        public string MembershipType { get; set; } = MemberType.Student;

        [StringLength(100)]
        public string? Department { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [StringLength(30)]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Due date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Due Date")]
        public DateTime? DueDate { get; set; }
    }

    public sealed record PublicBookDetails(
        int Id,
        string Title,
        string Author,
        string Category,
        int PublishedYear,
        int AvailableCopies,
        string? CoverImagePath,
        string? Isbn);

    public sealed record BorrowConfirmation(
        string LoanId,
        string BookTitle,
        string BorrowerName,
        DateTime BorrowDate,
        DateTime DueDate,
        string StudentId);
}
