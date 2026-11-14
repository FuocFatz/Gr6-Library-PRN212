using PRN212.LibraryManagement.Controls;
using PRN212.LibraryManagement.Entities;
using System.Windows;

namespace PRN212.LibraryManagement;

public partial class CustomerWindow : Window
{
    public CustomerWindow(User currentUser)
    {
        InitializeComponent();
        TxtWelcome.Text = $"Xin chào, {currentUser.FullName}";
        CustomerContent.Content = new CustomerPortalControl(currentUser.UserId);
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        new MainWindow().Show();
        Close();
    }
}
