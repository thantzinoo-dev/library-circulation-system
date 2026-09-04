using System.ComponentModel.DataAnnotations;

namespace School_Library_Management.Models;

public class Book
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ISBN is required.")]
    [StringLength(20)]
    [Display(Name = "ISBN")]
    public string ISBN { get; set; } = string.Empty;

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Author is required.")]
    [StringLength(150)]
    public string Author { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Category { get; set; }

    [StringLength(50)]
    public string? Language { get; set; }

    [Range(1450, 2100, ErrorMessage = "Enter a published year between 1450 and 2100.")]
    [Display(Name = "Published Year")]
    public int PublishedYear { get; set; }

    [Range(1, int.MaxValue)]
    [Display(Name = "Total Copies")]
    public int TotalCopies { get; set; } = 1;

    [Range(0, int.MaxValue)]
    [Display(Name = "Available Copies")]
    public int AvailableCopies { get; set; } = 1;

    public DateTime CreatedAt { get; set; }

    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
