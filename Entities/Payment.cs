namespace PRN212.LibraryManagement.Entities;

public class Payment
{
    public int PaymentId { get; set; }
    public int BorrowId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentWay { get; set; } = "Cash";
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string PaymentStatus { get; set; } = "Completed";

    public BorrowRecord BorrowRecord { get; set; } = null!;
}
