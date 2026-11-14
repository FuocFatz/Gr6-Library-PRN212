namespace PRN212.LibraryManagement.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";

    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
