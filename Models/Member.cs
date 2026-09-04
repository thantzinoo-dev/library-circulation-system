using System.ComponentModel.DataAnnotations;

namespace School_Library_Management.Models;

public class Member
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Student ID is required.")]
    [StringLength(30)]
    [Display(Name = "Student ID")]
    public string StudentId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Department { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
