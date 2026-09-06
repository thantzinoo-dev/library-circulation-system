using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Library_Management.Pages.Return;

public class IndexModel : PageModel
{
    public IReadOnlyList<ReturnRow> RecentReturns { get; } =
    [
        new("LOAN0001", "Aung Min Thant", "Clean Code", "Jun 01, 2025", "0 MMK", "Good"),
        new("LOAN0002", "Su Su Hlaing", "Atomic Habits", "May 31, 2025", "2,000 MMK", "Late"),
        new("LOAN0003", "Ko Ko Zaw", "Deep Work", "May 29, 2025", "0 MMK", "Good"),
        new("LOAN0004", "Hnin Ei Phyu", "The Alchemist", "May 28, 2025", "5,000 MMK", "Damaged"),
        new("LOAN0005", "Thandar Win", "The 5 AM Club", "May 26, 2025", "0 MMK", "Good"),
        new("LOAN0006", "Khant Sithu Aung", "Rich Dad Poor Dad", "May 23, 2025", "2,000 MMK", "Late")
    ];

    public void OnGet()
    {
    }

    public sealed record ReturnRow(
        string LoanId,
        string MemberName,
        string BookTitle,
        string ReturnDate,
        string Fine,
        string Condition);
}
