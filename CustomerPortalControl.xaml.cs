using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using PRN212.LibraryManagement.Entities;
using PRN212.LibraryManagement.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PRN212.LibraryManagement.Controls;

public partial class CustomerPortalControl : UserControl
{
    private readonly int _customerId;
    private List<Book> _allBooks = new();
    private Book? _selectedBookForBorrow;
    private const decimal FeePerDay = 5000m;

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

    private async void RefreshBooks_Click(object sender, RoutedEventArgs e)
    {
        TxtSearchBook.Clear();
        await LoadBooksAsync();
    }

    private async void RefreshHistory_Click(object sender, RoutedEventArgs e)
    {
        await LoadHistoryAsync();
    }

    private void TxtSearchBook_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyBookFilter();
    }

    private void ApplyBookFilter()
    {
        string keyword = TxtSearchBook.Text.Trim().ToLower();
        if (string.IsNullOrEmpty(keyword))
        {
            BooksGrid.ItemsSource = _allBooks;
            return;
        }

        BooksGrid.ItemsSource = _allBooks
            .Where(b => (b.BookTitle != null && b.BookTitle.ToLower().Contains(keyword))
                     || (b.BookCategory != null && b.BookCategory.ToLower().Contains(keyword)))
            .ToList();
    }

    private async Task LoadBooksAsync()
    {
        try
        {
            using var db = new LibraryDbContext();
            _allBooks = await db.Books.AsNoTracking()
                .OrderBy(b => b.BookTitle)
                .ToListAsync();

            ApplyBookFilter();
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
            var history = await db.BorrowRecords.AsNoTracking()
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

            HistoryGrid.ItemsSource = history;
            TxtEmptyHistory.Visibility = history.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải lịch sử mượn.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex));
        }
    }

    // --- XỬ LÝ TẠO LƯỢT MƯỢN & THANH TOÁN ---

    private void OpenBorrowModal_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.DataContext is not Book book)
            return;

        if (book.AvailableQuantity <= 0)
        {
            MessageBox.Show("Cuốn sách này hiện đã hết trong kho, vui lòng chọn sách khác.",
                            "Hết sách", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _selectedBookForBorrow = book;
        TxtModalBookTitle.Text = $"{book.BookTitle} ({book.BookCategory})";
        CboBorrowDays.SelectedIndex = 1; // Mặc định 7 ngày
        UpdateModalCalculation();

        BorrowPaymentOverlay.Visibility = Visibility.Visible;
    }

    private void CboBorrowDays_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateModalCalculation();
    }

    private void UpdateModalCalculation()
    {
        if (TxtModalDueDate == null || TxtModalTotalFee == null)
            return;

        int days = GetSelectedBorrowDays();
        DateTime dueDate = DateTime.Now.AddDays(days);
        decimal totalFee = days * FeePerDay;

        TxtModalDueDate.Text = dueDate.ToString("dd/MM/yyyy");
        TxtModalTotalFee.Text = $"{totalFee:N0} VNĐ";
    }

    private int GetSelectedBorrowDays()
    {
        if (CboBorrowDays.SelectedItem is ComboBoxItem item && int.TryParse(item.Tag?.ToString(), out int days))
            return days;
        return 7;
    }

    private void CancelBorrowModal_Click(object sender, RoutedEventArgs e)
    {
        _selectedBookForBorrow = null;
        BorrowPaymentOverlay.Visibility = Visibility.Collapsed;
    }

    private async void ConfirmBorrowPayment_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBookForBorrow == null)
            return;

        BtnConfirmPayment.IsEnabled = false;

        try
        {
            using var db = new LibraryDbContext();
            using var transaction = await db.Database.BeginTransactionAsync();

            var bookInDb = await db.Books.FirstOrDefaultAsync(b => b.BookId == _selectedBookForBorrow.BookId);
            if (bookInDb == null || bookInDb.AvailableQuantity <= 0)
            {
                MessageBox.Show("Rất tiếc, sách vừa hết số lượng trong kho!", "Thông báo",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int days = GetSelectedBorrowDays();
            string paymentMethod = (CboPaymentMethod.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Chuyển khoản";

            // 1. Trừ số lượng sách còn lại
            bookInDb.AvailableQuantity -= 1;

            // 2. Tạo bản ghi mượn mới cho tài khoản hiện tại
            var newRecord = new BorrowRecord
            {
                UserId = _customerId,
                BookId = bookInDb.BookId,
                BorrowDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(days),
                ReturnDate = null,
                Status = "Borrowed"
            };

            db.BorrowRecords.Add(newRecord);
            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            BorrowPaymentOverlay.Visibility = Visibility.Collapsed;
            _selectedBookForBorrow = null;

            MessageBox.Show($"Thanh toán thành công qua [{paymentMethod}]!\n" +
                            $"Sách \"{bookInDb.BookTitle}\" đã được thêm vào lịch sử mượn của bạn.",
                            "Hoàn tất giao dịch", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadBooksAsync();
            await LoadHistoryAsync();
            MainTabControl.SelectedIndex = 1;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Giao dịch thất bại.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                            "Lỗi thanh toán", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnConfirmPayment.IsEnabled = true;
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
