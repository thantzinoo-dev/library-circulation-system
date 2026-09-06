using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Library_Management.Pages.Reports;

public class IndexModel : PageModel
{
    public IReadOnlyList<MonthlyBorrowing> MonthlyBorrowings { get; } =
    [
        new("Jan", 180),
        new("Feb", 210),
        new("Mar", 250),
        new("Apr", 220),
        new("May", 240),
        new("Jun", 256)
    ];

    public IReadOnlyList<CategoryShare> CategoryShares { get; } =
    [
        new("Programming", 30, "#1468E8"),
        new("Fiction", 25, "#20B97A"),
        new("Self-Help", 20, "#F59E0B"),
        new("Business", 15, "#7C3AED"),
        new("Computer Science", 10, "#22A7C7")
    ];

    public IReadOnlyList<PopularBook> PopularBooks { get; } =
    [
        new("Clean Code", "Programming", 32, 18, "0 MMK", "CC"),
        new("Atomic Habits", "Self-Help", 28, 5, "3,000 MMK", "AH"),
        new("Deep Work", "Productivity", 21, 2, "5,000 MMK", "DW"),
        new("The Alchemist", "Fiction", 19, 12, "0 MMK", "TA"),
        new("Rich Dad Poor Dad", "Finance", 17, 3, "2,000 MMK", "RD")
    ];

    public void OnGet()
    {
    }

    public sealed record MonthlyBorrowing(string Month, int Count);
    public sealed record CategoryShare(string Label, int Percentage, string Color);
    public sealed record PopularBook(
        string Title,
        string Category,
        int TotalBorrowed,
        int AvailableCopies,
        string FineGenerated,
        string Initials);
}
