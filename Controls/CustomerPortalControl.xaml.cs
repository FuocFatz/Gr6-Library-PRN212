using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using PRN212.LibraryManagement.Services;
using PRN212.LibraryManagement.Entities;
using System.Windows;
using System.Windows.Controls;

namespace PRN212.LibraryManagement.Controls;

public partial class CustomerPortalControl : UserControl
{
    private readonly int _customerId;

    public CustomerPortalControl(int customerId)
    {
        InitializeComponent();
        _customerId = customerId;
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadBooksAsync();
        await LoadHistoryAsync();
    }

    private async void RefreshBooks_Click(object sender, RoutedEventArgs e) => await LoadBooksAsync();

    private async Task LoadBooksAsync()
    {
        try
        {
            using var db = new LibraryDbContext();
            BooksGrid.ItemsSource = await db.Books.AsNoTracking()
                .OrderBy(b => b.BookTitle)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải danh mục sách.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex));
        }
    }

    private async Task LoadHistoryAsync()
    {
        try
        {
            using var db = new LibraryDbContext();
            HistoryGrid.ItemsSource = await db.BorrowRecords.AsNoTracking()
                .Where(r => r.UserId == _customerId)
                .Include(r => r.Book)
                .OrderByDescending(r => r.BorrowDate)
                .Select(r => new CustomerHistoryRow
                {
                    BorrowId = r.BorrowId,
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
            MessageBox.Show("Không thể tải lịch sử mượn.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex));
        }
    }

    private sealed class CustomerHistoryRow
    {
        public int BorrowId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
