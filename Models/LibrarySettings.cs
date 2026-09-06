using System.ComponentModel.DataAnnotations;

namespace School_Library_Management.Models;

public class LibrarySettings
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    [Display(Name = "Default report period")]
    public string DefaultReportPeriod { get; set; } = ReportPeriod.ThisMonth;

    [Required]
    [StringLength(20)]
    [Display(Name = "Theme")]
    public string Theme { get; set; } = ThemePreference.System;

    [Display(Name = "Show PDF export")]
    public bool ShowPdfExport { get; set; } = true;

    [Display(Name = "Show Excel export")]
    public bool ShowExcelExport { get; set; } = true;

    public DateTime UpdatedAt { get; set; }
}

public static class ReportPeriod
{
    public const string ThisMonth = "ThisMonth";
    public const string LastMonth = "LastMonth";
    public const string ThisYear = "ThisYear";
    public const string All = "All";

    public static readonly string[] AllValues = [ThisMonth, LastMonth, ThisYear, All];
}

public static class ThemePreference
{
    public const string System = "System";
    public const string Light = "Light";
    public const string Dark = "Dark";

    public static readonly string[] AllValues = [System, Light, Dark];
}
