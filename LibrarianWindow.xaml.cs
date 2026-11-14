using PRN212.LibraryManagement.Entities;
using System.Windows;

namespace PRN212.LibraryManagement;

public partial class LibrarianWindow : Window
{
    public LibrarianWindow(User currentUser)
    {
        InitializeComponent();
        TxtWelcome.Text = $"Xin chào, {currentUser.FullName} (Thủ thư)";
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }
}
