using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Members;

public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public const int PageSize = 10;

    public IReadOnlyList<MemberViewModel> Members { get; private set; } = Array.Empty<MemberViewModel>();

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? MembershipType { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Type { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    public int TotalItems { get; private set; }
    public int TotalPages { get; private set; }
    public int StartItem { get; private set; }
    public int EndItem { get; private set; }
    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public class MemberViewModel
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Department { get; set; }
        public string MembershipType { get; set; } = MemberType.Student;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int BorrowCount { get; set; }
        public int ActiveBorrowCount { get; set; }
    }

    public async Task OnGetAsync()
    {
        IQueryable<Member> query = _context.Members.AsNoTracking();

        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        if (Search is not null)
        {
            var searchPattern = Search;
            var cleanPhone = Search.Replace(" ", string.Empty).Replace("-", string.Empty);

            query = query.Where(m =>
                m.StudentId.Contains(searchPattern) ||
                m.Name.Contains(searchPattern) ||
                (m.Email != null && m.Email.Contains(searchPattern)) ||
                (m.Phone != null && (m.Phone.Contains(searchPattern) || (cleanPhone.Length > 0 && m.Phone.Replace(" ", string.Empty).Replace("-", string.Empty).Contains(cleanPhone)))) ||
                (m.Department != null && m.Department.Contains(searchPattern)));
        }

        if (!string.IsNullOrWhiteSpace(Status) && !Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            if (Status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => m.IsActive);
            }
            else if (Status.Equals("Inactive", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(m => !m.IsActive);
            }
        }

        var effectiveType = !string.IsNullOrWhiteSpace(MembershipType) ? MembershipType : Type;
        if (!string.IsNullOrWhiteSpace(effectiveType) && !effectiveType.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            MembershipType = effectiveType;
            Type = effectiveType;
            query = query.Where(m => m.MembershipType == effectiveType);
        }

        TotalItems = await query.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);
        if (TotalPages < 1)
        {
            TotalPages = 1;
        }

        if (PageIndex < 1)
        {
            PageIndex = 1;
        }
        else if (PageIndex > TotalPages)
        {
            PageIndex = TotalPages;
        }

        StartItem = TotalItems == 0 ? 0 : (PageIndex - 1) * PageSize + 1;
        EndItem = Math.Min(PageIndex * PageSize, TotalItems);

        var memberEntities = await query
            .OrderByDescending(m => m.CreatedAt)
            .ThenBy(m => m.StudentId)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .Select(m => new MemberViewModel
            {
                Id = m.Id,
                StudentId = m.StudentId,
                Name = m.Name,
                Email = m.Email,
                Phone = m.Phone,
                Department = m.Department,
                MembershipType = m.MembershipType,
                IsActive = m.IsActive,
                CreatedAt = m.CreatedAt,
                BorrowCount = m.BorrowRecords.Count,
                ActiveBorrowCount = m.BorrowRecords.Count(r => r.Status != BorrowStatus.Returned)
            })
            .ToListAsync();

        Members = memberEntities;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var member = await _context.Members.FindAsync(id);
        if (member is null)
        {
            ErrorMessage = "The member could not be found.";
            return RedirectToPage(new { Search, Status, MembershipType, PageIndex });
        }

        var hasBorrowHistory = await _context.BorrowRecords.AnyAsync(r => r.MemberId == id);
        if (hasBorrowHistory)
        {
            ErrorMessage = "This member has borrowing history and cannot be deleted. Set the member to Inactive instead.";
            return RedirectToPage(new { Search, Status, MembershipType, PageIndex });
        }

        _context.Members.Remove(member);

        try
        {
            await _context.SaveChangesAsync();
            StatusMessage = $"Member “{member.Name}” ({member.StudentId}) was successfully deleted.";
        }
        catch (DbUpdateException)
        {
            ErrorMessage = "This member has borrowing history and cannot be deleted. Set the member to Inactive instead.";
        }

        return RedirectToPage(new { Search, Status, MembershipType, PageIndex });
    }
}
