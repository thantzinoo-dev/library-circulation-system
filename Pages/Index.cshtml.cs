using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    public string CurrentDateFormatted { get; private set; } = string.Empty;
    public string CurrentDateIso { get; private set; } = string.Empty;
    public int TotalBooks { get; private set; }
    public int BooksAddedThisMonth { get; private set; }
    public int TotalMembers { get; private set; }
    public int MembersAddedThisMonth { get; private set; }
    public int BooksBorrowed { get; private set; }
    public int BorrowedThisWeek { get; private set; }
    public int OverdueBooks { get; private set; }
    public int NewlyOverdueToday { get; private set; }
    public decimal TotalFeesCollected { get; private set; }
    public decimal FeesCollectedThisMonth { get; private set; }
    public int ChartMaximum { get; private set; } = 10;
    public string BorrowingChartPoints { get; private set; } = string.Empty;
    public string BorrowingChartAreaPath { get; private set; } = string.Empty;
    public IReadOnlyList<DailyBorrowing> DailyBorrowings { get; private set; } = [];
    public IReadOnlyList<ChartLabel> ChartLabels { get; private set; } = [];
    public IReadOnlyList<RecentBorrowing> RecentBorrowings { get; private set; } = [];
    public IReadOnlyList<OverdueBook> OverdueItems { get; private set; } = [];
    public IReadOnlyList<MemberSummary> MemberSummaries { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var nextMonth = monthStart.AddMonths(1);
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));

        CurrentDateFormatted = today.ToString("MMMM d, yyyy (dddd)", CultureInfo.InvariantCulture);
        CurrentDateIso = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        TotalBooks = await _context.Books.AsNoTracking().SumAsync(b => (int?)b.TotalCopies) ?? 0;
        BooksAddedThisMonth = await _context.Books.AsNoTracking()
            .Where(b => b.CreatedAt >= monthStart && b.CreatedAt < nextMonth)
            .SumAsync(b => (int?)b.TotalCopies) ?? 0;
        TotalMembers = await _context.Members.AsNoTracking().CountAsync();
        MembersAddedThisMonth = await _context.Members.AsNoTracking()
            .CountAsync(m => m.CreatedAt >= monthStart && m.CreatedAt < nextMonth);
        BooksBorrowed = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        BorrowedThisWeek = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.BorrowDate >= weekStart && r.BorrowDate < tomorrow)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        OverdueBooks = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null && r.DueDate < today)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        NewlyOverdueToday = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate == null && r.DueDate >= today.AddDays(-1) && r.DueDate < today)
            .SumAsync(r => (int?)r.Quantity) ?? 0;
        TotalFeesCollected = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate != null)
            .SumAsync(r => (decimal?)r.FineAmount) ?? 0m;
        FeesCollectedThisMonth = await _context.BorrowRecords.AsNoTracking()
            .Where(r => r.ReturnDate >= monthStart && r.ReturnDate < nextMonth)
            .SumAsync(r => (decimal?)r.FineAmount) ?? 0m;

        await LoadBorrowingChartAsync(monthStart, nextMonth);
        await LoadRecentBorrowingsAsync(today);
        await LoadOverdueBooksAsync(today);
        await LoadMemberSummaryAsync();
    }

    private async Task LoadBorrowingChartAsync(DateTime monthStart, DateTime nextMonth)
    {
        var raw = await _context.BorrowRecords
            .AsNoTracking()
            .Where(r => r.BorrowDate >= monthStart && r.BorrowDate < nextMonth)
            .Select(r => new { r.BorrowDate, r.Quantity })
            .ToListAsync();

        var days = DateTime.DaysInMonth(monthStart.Year, monthStart.Month);
        var counts = Enumerable.Range(1, days)
            .Select(day => raw.Where(r => r.BorrowDate.Day == day).Sum(r => r.Quantity))
            .ToArray();
        var maximum = counts.Length == 0 ? 0 : counts.Max();
        ChartMaximum = Math.Max(10, (int)Math.Ceiling(maximum / 10d) * 10);

        const double left = 60;
        const double right = 700;
        const double top = 35;
        const double bottom = 267;
        var width = right - left;
        var height = bottom - top;

        DailyBorrowings = Enumerable.Range(1, days)
            .Select(day =>
            {
                var count = counts[day - 1];
                var x = days == 1 ? left : left + ((day - 1d) / (days - 1d) * width);
                var y = bottom - (count / (double)ChartMaximum * height);
                return new DailyBorrowing(day, count, x, y);
            })
            .ToList();

        BorrowingChartPoints = string.Join(" ", DailyBorrowings.Select(point => FormattableString.Invariant($"{point.X:0.##},{point.Y:0.##}")));
        BorrowingChartAreaPath = DailyBorrowings.Count == 0
            ? string.Empty
            : $"M {BorrowingChartPoints.Replace(" ", " L ")} L {right},267 L {left},267 Z";

        var labelDays = new[] { 1, 6, 11, 16, 21, 26, days }.Distinct().Where(day => day <= days);
        ChartLabels = labelDays.Select(day =>
        {
            var x = days == 1 ? left : left + ((day - 1d) / (days - 1d) * width);
            return new ChartLabel($"{monthStart:MMM} {day}", x);
        }).ToList();
    }

    private async Task LoadRecentBorrowingsAsync(DateTime today)
    {
        var rows = await _context.BorrowRecords
            .AsNoTracking()
            .OrderByDescending(r => r.BorrowDate)
            .ThenByDescending(r => r.Id)
            .Take(5)
            .Select(r => new
            {
                BookTitle = r.Book.Title,
                MemberName = r.Member.Name,
                r.BorrowDate,
                r.DueDate,
                r.ReturnDate
            })
            .ToListAsync();

        RecentBorrowings = rows.Select(row => new RecentBorrowing(
            row.BookTitle,
            row.MemberName,
            row.BorrowDate,
            row.ReturnDate is not null ? BorrowStatus.Returned : row.DueDate < today ? BorrowStatus.Overdue : BorrowStatus.Borrowed,
            Initials(row.BookTitle))).ToList();
    }

    private async Task LoadOverdueBooksAsync(DateTime today)
    {
        var rows = await _context.BorrowRecords
            .AsNoTracking()
            .Where(r => r.ReturnDate == null && r.DueDate < today)
            .OrderBy(r => r.DueDate)
            .ThenBy(r => r.Id)
            .Take(3)
            .Select(r => new { BookTitle = r.Book.Title, MemberName = r.Member.Name, r.DueDate })
            .ToListAsync();

        OverdueItems = rows.Select(row => new OverdueBook(
            row.BookTitle,
            row.MemberName,
            Math.Max(1, (today - row.DueDate.Date).Days),
            Initials(row.BookTitle))).ToList();
    }

    private async Task LoadMemberSummaryAsync()
    {
        var groups = await _context.Members
            .AsNoTracking()
            .GroupBy(member => member.MembershipType)
            .Select(group => new { Type = group.Key, Count = group.Count() })
            .ToListAsync();
        var total = groups.Sum(group => group.Count);
        var colors = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [MemberType.Student] = "#6557E8",
            [MemberType.Teacher] = "#2785EA",
            [MemberType.Staff] = "#3DCB91"
        };

        var offset = 0d;
        MemberSummaries = new[] { MemberType.Student, MemberType.Teacher, MemberType.Staff }
            .Select(type =>
            {
                var count = groups.FirstOrDefault(group => group.Type == type)?.Count ?? 0;
                var percentage = total == 0 ? 0d : count * 100d / total;
                var item = new MemberSummary(type, count, percentage, offset, colors[type]);
                offset += percentage;
                return item;
            }).ToList();
    }

    private static string Initials(string title) => string.Concat(title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => char.ToUpperInvariant(word[0])));

    public sealed record DailyBorrowing(int Day, int Count, double X, double Y);
    public sealed record ChartLabel(string Text, double X);
    public sealed record RecentBorrowing(string BookTitle, string MemberName, DateTime BorrowDate, string Status, string Initials);
    public sealed record OverdueBook(string BookTitle, string MemberName, int DaysOverdue, string Initials);
    public sealed record MemberSummary(string Type, int Count, double Percentage, double Offset, string Color);
}
