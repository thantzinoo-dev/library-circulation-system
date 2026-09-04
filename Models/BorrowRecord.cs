using System.ComponentModel.DataAnnotations;

namespace School_Library_Management.Models;

public class BorrowRecord
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public Book Book { get; set; } = null!;

    public int MemberId { get; set; }

    public Member Member { get; set; } = null!;

    [Display(Name = "Borrow Date")]
    public DateTime BorrowDate { get; set; }

    [Display(Name = "Due Date")]
    public DateTime DueDate { get; set; }

    [Display(Name = "Return Date")]
    public DateTime? ReturnDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = BorrowStatus.Borrowed;

    [Display(Name = "Fine Amount")]
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal FineAmount { get; set; }
}
