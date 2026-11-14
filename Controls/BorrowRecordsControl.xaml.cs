using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using PRN212.LibraryManagement.Services;
using System.Windows;
using System.Windows.Controls;

namespace PRN212.LibraryManagement.Controls;

public partial class BorrowRecordsControl : UserControl
{
    public BorrowRecordsControl()
    {
        InitializeComponent();
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e) => await LoadRecordsAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadRecordsAsync();

    private async Task LoadRecordsAsync()
    {
        try
        {
            using var db = new LibraryDbContext();
            BorrowGrid.ItemsSource = await db.BorrowRecords.AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Book)
                .OrderByDescending(r => r.BorrowDate)
                .ThenByDescending(r => r.BorrowId)
                .Select(r => new BorrowRow
                {
                    BorrowId = r.BorrowId,
                    CustomerName = r.User.FullName,
                    BookTitle = r.Book.BookTitle,
                    BorrowDate = r.BorrowDate,
                    DueDate = r.DueDate,
                    ReturnDate = r.ReturnDate,
                    Status = r.Status
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải lịch sử mượn.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed class BorrowRow
    {
        public int BorrowId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
