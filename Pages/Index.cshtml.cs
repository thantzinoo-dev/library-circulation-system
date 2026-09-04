using System.Globalization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace School_Library_Management.Pages;

public class IndexModel : PageModel
{
    public string CurrentDateFormatted { get; private set; } = string.Empty;
    public string CurrentDateIso { get; private set; } = string.Empty;

    public void OnGet()
    {
        var now = DateTime.Now;
        CurrentDateFormatted = now.ToString("MMMM d, yyyy (dddd)", CultureInfo.InvariantCulture);
        CurrentDateIso = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
