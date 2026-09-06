using System.ComponentModel.DataAnnotations;

namespace School_Library_Management.Models;

public class AdminProfile
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = "Admin User";

    [Required(ErrorMessage = "Role is required.")]
    [StringLength(60)]
    public string Role { get; set; } = "Administrator";

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    [StringLength(500)]
    public string? Bio { get; set; }

    public DateTime UpdatedAt { get; set; }
}
