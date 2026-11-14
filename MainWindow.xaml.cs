using Microsoft.EntityFrameworkCore;
using PRN212.LibraryManagement.Data;
using PRN212.LibraryManagement.Entities;
using PRN212.LibraryManagement.Services;
using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;

namespace PRN212.LibraryManagement;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void ShowRegister_Click(object sender, RoutedEventArgs e)
    {
        LoginPanel.Visibility = Visibility.Collapsed;
        RegisterPanel.Visibility = Visibility.Visible;
    }

    private void ShowLogin_Click(object sender, RoutedEventArgs e)
    {
        RegisterPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
    }

    private async void TestDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var db = new LibraryDbContext();
            string? problem = await DatabaseDiagnostics.ValidateSchemaAsync(db);

            if (problem is null)
            {
                MessageBox.Show(
                    "Kết nối SQL Server thành công. Đã tìm thấy đủ 4 bảng và các cột cần thiết.",
                    "Kiểm tra database",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(problem, "Database chưa đúng cấu trúc", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể kết nối database.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi kết nối SQL Server",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        string username = TxtLoginUsername.Text.Trim();
        string password = PwbLoginPassword.Password;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Thiếu thông tin",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var db = new LibraryDbContext();
            string? schemaProblem = await DatabaseDiagnostics.ValidateSchemaAsync(db);
            if (schemaProblem is not null)
            {
                MessageBox.Show(schemaProblem, "Database chưa đúng cấu trúc",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            User? user = await db.Users.AsNoTracking()
                .SingleOrDefaultAsync(u => u.Username == username);

            if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
            {
                MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.", "Đăng nhập thất bại",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Window nextWindow = user.Role switch
            {
                "Admin" => new AdminWindow(user),
                "Librarian" => new LibrarianWindow(user),
                "Customer" => new CustomerWindow(user),
                _ => throw new InvalidOperationException(
                    $"Vai trò '{user.Role}' không hợp lệ. Role phải là Admin, Librarian hoặc Customer.")
            };

            nextWindow.Show();
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể đăng nhập. Hãy kiểm tra SQL Server và appsettings.json.\n\n" +
                DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi đăng nhập",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private async void Register_Click(object sender, RoutedEventArgs e)
    {
        string fullName = TxtFullName.Text.Trim();
        string email = TxtEmail.Text.Trim();
        string username = TxtRegisterUsername.Text.Trim();
        string password = PwbRegisterPassword.Password;
        string confirmPassword = PwbConfirmPassword.Password;
        string role = (CmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Customer";

        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Vui lòng nhập đầy đủ họ tên, email, tên đăng nhập và mật khẩu.",
                "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (fullName.Length > 150 || email.Length > 150 || username.Length > 50)
        {
            MessageBox.Show(
                "Độ dài thông tin vượt giới hạn: Họ tên tối đa 150 ký tự, Email 150 ký tự, Tên đăng nhập 50 ký tự.",
                "Thông tin quá dài", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _ = new MailAddress(email);
        }
        catch (FormatException)
        {
            MessageBox.Show("Email không đúng định dạng.", "Email không hợp lệ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (password.Length < 6)
        {
            MessageBox.Show("Mật khẩu cần có ít nhất 6 ký tự.", "Mật khẩu chưa hợp lệ",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (password != confirmPassword)
        {
            MessageBox.Show("Mật khẩu nhập lại không khớp.", "Kiểm tra mật khẩu",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (role is not ("Admin" or "Librarian" or "Customer"))
        {
            MessageBox.Show("Vai trò không hợp lệ.");
            return;
        }

        BtnRegister.IsEnabled = false;

        try
        {
            using var db = new LibraryDbContext();

            string? schemaProblem = await DatabaseDiagnostics.ValidateSchemaAsync(db);
            if (schemaProblem is not null)
            {
                MessageBox.Show(schemaProblem, "Database chưa đúng cấu trúc",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool duplicateUsername = await db.Users.AnyAsync(u => u.Username == username);
            if (duplicateUsername)
            {
                MessageBox.Show("Tên đăng nhập này đã được sử dụng. Hãy chọn tên khác.",
                    "Trùng tên đăng nhập", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool duplicateEmail = await db.Users.AnyAsync(u => u.Email == email);
            if (duplicateEmail)
            {
                MessageBox.Show("Email này đã được sử dụng. Hãy dùng email khác.",
                    "Trùng email", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = new User
            {
                Username = username,
                PasswordHash = PasswordHasher.Hash(password),
                FullName = fullName,
                Email = email,
                Role = role
            };

            db.Users.Add(user);
            await db.SaveChangesAsync();

            MessageBox.Show($"Tạo tài khoản {role} thành công! Bạn có thể đăng nhập ngay.",
                "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

            TxtLoginUsername.Text = username;
            PwbLoginPassword.Password = string.Empty;
            RegisterPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            ClearRegisterFields();
        }
        catch (DbUpdateException ex)
        {
            string detail = DatabaseDiagnostics.GetDetailedMessage(ex);
            MessageBox.Show(
                "SQL Server từ chối lưu tài khoản. Nguyên nhân chi tiết:\n\n" + detail +
                "\n\nHãy kiểm tra database có đúng 4 bảng theo file SQL bản chốt hay không.",
                "Lỗi lưu tài khoản", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "Không thể tạo tài khoản.\n\n" + DatabaseDiagnostics.GetDetailedMessage(ex),
                "Lỗi đăng ký", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnRegister.IsEnabled = true;
        }
    }

    private void ClearRegisterFields()
    {
        TxtFullName.Clear();
        TxtEmail.Clear();
        TxtRegisterUsername.Clear();
        PwbRegisterPassword.Clear();
        PwbConfirmPassword.Clear();
        CmbRole.SelectedIndex = 2;
    }
}
