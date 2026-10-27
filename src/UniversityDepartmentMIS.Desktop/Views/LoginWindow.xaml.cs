using System.Windows;
using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly AppServices _services;

    public LoginWindow(AppServices services)
    {
        InitializeComponent();
        _services = services;
        Loaded += (_, _) => UsernameInput.Focus();
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        var username = UsernameInput.Text;
        var password = PasswordInput.Password;

        SetBusy(true);
        AuthResult result;
        try
        {
            // PBKDF2 with 600,000 iterations takes a moment, so keep it off the UI thread.
            result = await Task.Run(() => _services.Auth.Login(username, password));
        }
        finally
        {
            SetBusy(false);
        }

        if (!result.Succeeded || result.User is null)
        {
            ErrorText.Text = result.Error;
            ErrorText.Visibility = Visibility.Visible;
            PasswordInput.Clear();
            PasswordInput.Focus();
            return;
        }

        var main = new MainWindow(_services, result.User);
        Application.Current.MainWindow = main;
        main.Show();
        Close();
    }

    private void SetBusy(bool busy)
    {
        SignInButton.IsEnabled = !busy;
        UsernameInput.IsEnabled = !busy;
        PasswordInput.IsEnabled = !busy;
    }
}
