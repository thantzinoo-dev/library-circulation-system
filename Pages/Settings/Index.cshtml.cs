using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using School_Library_Management.Data;
using School_Library_Management.Models;

namespace School_Library_Management.Pages.Settings;

public class IndexModel(ApplicationDbContext context) : PageModel
{
    private readonly ApplicationDbContext _context = context;

    [BindProperty]
    public SettingsInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await _context.LibrarySettings.AsNoTracking().OrderBy(setting => setting.Id).FirstOrDefaultAsync();
        if (settings is null)
        {
            return;
        }

        Input = new SettingsInput
        {
            DefaultReportPeriod = settings.DefaultReportPeriod,
            Theme = settings.Theme,
            ShowPdfExport = settings.ShowPdfExport,
            ShowExcelExport = settings.ShowExcelExport
        };
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ReportPeriod.AllValues.Contains(Input.DefaultReportPeriod))
        {
            ModelState.AddModelError("Input.DefaultReportPeriod", "Select a valid report period.");
        }

        if (!ThemePreference.AllValues.Contains(Input.Theme))
        {
            ModelState.AddModelError("Input.Theme", "Select a valid theme preference.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var settings = await _context.LibrarySettings.OrderBy(setting => setting.Id).FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new LibrarySettings();
            _context.LibrarySettings.Add(settings);
        }

        settings.DefaultReportPeriod = Input.DefaultReportPeriod;
        settings.Theme = Input.Theme;
        settings.ShowPdfExport = Input.ShowPdfExport;
        settings.ShowExcelExport = Input.ShowExcelExport;
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        StatusMessage = "Settings saved successfully.";
        return RedirectToPage();
    }

    public sealed class SettingsInput
    {
        [Required]
        [Display(Name = "Default report period")]
        public string DefaultReportPeriod { get; set; } = ReportPeriod.ThisMonth;

        [Required]
        public string Theme { get; set; } = ThemePreference.System;

        [Display(Name = "Show PDF export")]
        public bool ShowPdfExport { get; set; } = true;

        [Display(Name = "Show Excel export")]
        public bool ShowExcelExport { get; set; } = true;
    }
}
