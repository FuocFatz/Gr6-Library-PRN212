using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using PRN212.LibraryManagement.Services;
using PRN212.LibraryManagement.Entities;
using System.Windows;
using System.Windows.Controls;

namespace PRN212.LibraryManagement.Controls;

public partial class BookManagementControl : UserControl
{
    private int? _selectedBookId;
    private bool _isLoading;

    public BookManagementControl()
    {
        InitializeComponent();
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e)
    {
        if (!_isLoading)
            await LoadBooksAsync();
    }

    private async Task LoadBooksAsync(string? keyword = null)
    {
        try
        {
            _isLoading = true;
            using var db = new LibraryDbContext();
            IQueryable<Book> query = db.Books.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(b => b.BookTitle.Contains(keyword) ||
                                         b.BookCategory.Contains(keyword));
            }

            BooksGrid.ItemsSource = await query.OrderBy(b => b.BookTitle).ToListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải danh sách sách.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async void Search_Click(object sender, RoutedEventArgs e)
        => await LoadBooksAsync(TxtSearch.Text.Trim());

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        TxtSearch.Clear();
        await LoadBooksAsync();
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadBookInput(out string title, out string category, out int total))
            return;

        try
        {
            using var db = new LibraryDbContext();
            db.Books.Add(new Book
            {
                BookTitle = title,
                BookCategory = category,
                TotalQuantity = total,
                AvailableQuantity = total
            });
            await db.SaveChangesAsync();
            ClearForm();
            await LoadBooksAsync();
            MessageBox.Show("Thêm sách thành công.", "Thông báo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể thêm sách.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Update_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBookId is null)
        {
            MessageBox.Show("Hãy chọn một sách trong danh sách trước.");
            return;
        }
        if (!TryReadBookInput(out string title, out string category, out int total))
            return;

        try
        {
            using var db = new LibraryDbContext();
            Book? book = await db.Books.FirstOrDefaultAsync(b => b.BookId == _selectedBookId.Value);
            if (book is null)
            {
                MessageBox.Show("Sách không còn tồn tại. Hãy tải lại danh sách.");
                await LoadBooksAsync();
                return;
            }

            int borrowedCount = book.TotalQuantity - book.AvailableQuantity;
            if (total < borrowedCount)
            {
                MessageBox.Show($"Không thể đặt tổng số lượng nhỏ hơn số sách đang được mượn ({borrowedCount}).",
                    "Số lượng không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            book.BookTitle = title;
            book.BookCategory = category;
            book.TotalQuantity = total;
            // Giữ nguyên số sách đang được mượn; chỉ tính lại số còn khả dụng.
            book.AvailableQuantity = total - borrowedCount;

            await db.SaveChangesAsync();
            ClearForm();
            await LoadBooksAsync();
            MessageBox.Show("Cập nhật sách thành công.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể cập nhật sách.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedBookId is null)
        {
            MessageBox.Show("Hãy chọn sách cần xóa.");
            return;
        }

        if (MessageBox.Show("Bạn chắc chắn muốn xóa sách này?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        try
        {
            using var db = new LibraryDbContext();
            Book? book = await db.Books.FirstOrDefaultAsync(b => b.BookId == _selectedBookId.Value);
            if (book is null)
                return;

            bool hasHistory = await db.BorrowRecords.AnyAsync(r => r.BookId == book.BookId);
            if (hasHistory)
            {
                MessageBox.Show("Sách đã có lịch sử mượn nên không thể xóa để tránh mất dữ liệu. Bạn có thể đổi tên hoặc giữ sách trong danh sách.",
                    "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            db.Books.Remove(book);
            await db.SaveChangesAsync();
            ClearForm();
            await LoadBooksAsync();
            MessageBox.Show("Xóa sách thành công.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xóa sách.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BooksGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BooksGrid.SelectedItem is not Book book)
            return;

        _selectedBookId = book.BookId;
        TxtBookTitle.Text = book.BookTitle;
        TxtBookCategory.Text = book.BookCategory;
        TxtTotalQuantity.Text = book.TotalQuantity.ToString();
    }

    private void Clear_Click(object sender, RoutedEventArgs e) => ClearForm();

    private void ClearForm()
    {
        _selectedBookId = null;
        BooksGrid.SelectedItem = null;
        TxtBookTitle.Clear();
        TxtBookCategory.Clear();
        TxtTotalQuantity.Text = "1";
    }

    private bool TryReadBookInput(out string title, out string category, out int total)
    {
        title = TxtBookTitle.Text.Trim();
        category = TxtBookCategory.Text.Trim();
        total = 0;

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(category))
        {
            MessageBox.Show("Vui lòng nhập tên sách và thể loại.");
            return false;
        }

        if (!int.TryParse(TxtTotalQuantity.Text.Trim(), out total) || total < 1)
        {
            MessageBox.Show("Tổng số lượng phải là số nguyên từ 1 trở lên.");
            return false;
        }

        return true;
    }
}
