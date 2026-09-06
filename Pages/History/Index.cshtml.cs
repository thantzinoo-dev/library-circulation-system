using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Library_Management.Pages.History;

public class IndexModel : PageModel
{
    public IReadOnlyList<HistoryRow> Records { get; } =
    [
        new("LOAN0251", "Aung Min Thant", "Clean Code", "May 18, 2025", "Jun 01, 2025", null, "Borrowed", "0 MMK"),
        new("LOAN0252", "Su Su Hlaing", "Atomic Habits", "May 17, 2025", "May 31, 2025", "May 29, 2025", "Returned", "0 MMK"),
        new("LOAN0253", "Ko Ko Zaw", "Deep Work", "May 15, 2025", "May 29, 2025", null, "Overdue", "5,000 MMK"),
        new("LOAN0254", "Hnin Ei Phyu", "The Alchemist", "May 14, 2025", "May 28, 2025", "May 27, 2025", "Returned", "0 MMK"),
        new("LOAN0255", "Thandar Win", "The 5 AM Club", "May 12, 2025", "May 26, 2025", null, "Borrowed", "0 MMK"),
        new("LOAN0256", "Khant Sithu Aung", "Rich Dad Poor Dad", "May 09, 2025", "May 23, 2025", "May 23, 2025", "Returned", "0 MMK"),
        new("LOAN0257", "Ei Phyu Sin", "The Lean Startup", "May 07, 2025", "May 21, 2025", null, "Overdue", "2,000 MMK"),
        new("LOAN0258", "Zin Min Htet", "Clean Code", "May 05, 2025", "May 19, 2025", "May 18, 2025", "Returned", "0 MMK")
    ];

    public void OnGet()
    {
    }

    public sealed record HistoryRow(
        string LoanId,
        string MemberName,
        string BookTitle,
        string BorrowDate,
        string DueDate,
        string? ReturnDate,
        string Status,
        string Fine);
}
