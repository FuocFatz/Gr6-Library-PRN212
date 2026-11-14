namespace PRN212.LibraryManagement.Entities;

public class Book
{
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookCategory { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }

    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
