using PRN212.LibraryManagement.Controls;
using PRN212.LibraryManagement.Entities;
using System.Windows;

namespace PRN212.LibraryManagement;

public partial class AdminWindow : Window
{
    private readonly User _currentUser;

    public AdminWindow(User currentUser)
    {
        InitializeComponent();
        _currentUser = currentUser;
        TxtWelcome.Text = $"Xin chào, {_currentUser.FullName} (Admin)";
        UsersHost.Content = new UsersManagementControl(_currentUser.UserId);
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }
}
