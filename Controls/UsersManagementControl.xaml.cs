using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using PRN212.LibraryManagement.Services;
using System.Windows;
using System.Windows.Controls;

namespace PRN212.LibraryManagement.Controls;

public partial class UsersManagementControl : UserControl
{
    private readonly int _currentAdminId;

    public UsersManagementControl(int currentAdminId)
    {
        InitializeComponent();
        _currentAdminId = currentAdminId;
    }

    private async void Control_Loaded(object sender, RoutedEventArgs e) => await LoadUsersAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadUsersAsync();

    private async Task LoadUsersAsync()
    {
        try
        {
            using var db = new LibraryDbContext();
            UsersGrid.ItemsSource = await db.Users.AsNoTracking()
                .OrderBy(u => u.UserId)
                .Select(u => new UserRow
                {
                    UserId = u.UserId,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    Role = u.Role
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể tải danh sách tài khoản.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (UsersGrid.SelectedItem is not UserRow selected)
        {
            MessageBox.Show("Hãy chọn tài khoản cần xóa.");
            return;
        }

        if (selected.UserId == _currentAdminId)
        {
            MessageBox.Show("Bạn không thể xóa tài khoản đang đăng nhập.");
            return;
        }

        if (MessageBox.Show($"Xóa tài khoản '{selected.Username}'?", "Xác nhận",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        try
        {
            using var db = new LibraryDbContext();
            var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == selected.UserId);
            if (user is null)
                return;

            bool hasHistory = await db.BorrowRecords.AnyAsync(r => r.UserId == user.UserId);
            if (hasHistory)
            {
                MessageBox.Show("Tài khoản đã có lịch sử mượn sách nên không thể xóa để bảo toàn dữ liệu.",
                    "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            db.Users.Remove(user);
            await db.SaveChangesAsync();
            await LoadUsersAsync();
            MessageBox.Show("Đã xóa tài khoản.");
        }
        catch (Exception ex)
        {
            MessageBox.Show("Không thể xóa tài khoản.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private sealed class UserRow
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
