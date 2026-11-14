namespace PRN212.LibraryManagement.Entities;

public class BorrowRecord
{
    public int BorrowId { get; set; }
    public int UserId { get; set; }
    public int BookId { get; set; }
    public DateTime BorrowDate { get; set; } = DateTime.Today;
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = "Borrowed";

    public User User { get; set; } = null!;
    public Book Book { get; set; } = null!;
    public Payment? Payment { get; set; }
}
