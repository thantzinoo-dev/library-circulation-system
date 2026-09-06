using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Library_Management.Pages.Borrow;

public class IndexModel : PageModel
{
    public IReadOnlyList<LoanRow> RecentLoans { get; } =
    [
        new("LOAN0001", "Aung Min Thant", "Clean Code", "May 18, 2025", "Jun 01, 2025", "Issued"),
        new("LOAN0002", "Su Su Hlaing", "Atomic Habits", "May 17, 2025", "May 31, 2025", "Due Soon"),
        new("LOAN0003", "Ko Ko Zaw", "Deep Work", "May 15, 2025", "May 29, 2025", "Overdue"),
        new("LOAN0004", "Hnin Ei Phyu", "The Alchemist", "May 14, 2025", "May 28, 2025", "Issued"),
        new("LOAN0005", "Thandar Win", "The 5 AM Club", "May 12, 2025", "May 26, 2025", "Due Soon"),
        new("LOAN0006", "Khant Sithu Aung", "Rich Dad Poor Dad", "May 09, 2025", "May 23, 2025", "Overdue")
    ];

    public void OnGet()
    {
    }

    public sealed record LoanRow(
        string LoanId,
        string MemberName,
        string BookTitle,
        string BorrowDate,
        string DueDate,
        string Status);
}
