using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Core.Services;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

/// <summary>Admin screen: list accounts, add one, or reset a password. Passwords must meet <see cref="PasswordPolicy"/>.</summary>
public partial class UsersView : UserControl
{
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xB0, 0x00, 0x20));

    private readonly ScreenContext _context;

    public UsersView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;
        PolicyText.Text = "Passwords: " + PasswordPolicy.Description + " Only a salted hash is stored.";
        RoleInput.ItemsSource = Enum.GetValues<UserRole>();
        RoleInput.SelectedItem = UserRole.Staff;
        Refresh();
    }

    private User? SelectedUser => UsersGrid.SelectedItem as User;

    private void Refresh() => UsersGrid.ItemsSource = _context.Services.UserService.List();

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = SelectedUser;
        ResetHeader.Text = selected is null ? "Reset password" : $"Reset password for {selected.Username}";
        ResetPasswordInput.IsEnabled = selected is not null;
        ResetButton.IsEnabled = selected is not null;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var role = RoleInput.SelectedItem is UserRole chosen ? chosen : UserRole.Staff;
        var result = _context.Services.UserService.Create(UsernameInput.Text, NewPasswordInput.Password, role, _context.User.Department);
        if (Report(result, $"Added {UsernameInput.Text.Trim()}."))
        {
            UsernameInput.Clear();
            NewPasswordInput.Clear();
            Refresh();
        }
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedUser is not { } user)
        {
            return;
        }

        var result = _context.Services.UserService.ResetPassword(user.Id, ResetPasswordInput.Password);
        if (Report(result, $"Password changed for {user.Username}."))
        {
            ResetPasswordInput.Clear();
        }
    }

    private bool Report(OperationResult result, string success)
    {
        MessageList.Foreground = result.Succeeded ? Foreground : ErrorBrush;
        IEnumerable<string> messages = result.Succeeded ? [success] : result.Errors.Select(error => error.Message).ToList();
        MessageList.ItemsSource = messages;
        return result.Succeeded;
    }
}
