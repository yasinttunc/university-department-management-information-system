using System.Windows;
using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Navigation;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views;

public partial class MainWindow : Window
{
    private readonly AppServices _services;
    private readonly ScreenContext _context;
    private readonly List<NavigationMenuItem> _menu;
    private long? _pendingRecordId;

    public MainWindow(AppServices services, User user)
    {
        InitializeComponent();
        _services = services;
        _context = new ScreenContext(services, user, Navigate);

        UserText.Text = $"{user.Username} ({user.Role})";
        DepartmentText.Text = user.Department;

        _menu = ScreenCatalog.VisibleTo(user.Role).Select(NavigationMenuItem.From).ToList();
        MenuList.ItemsSource = _menu;
        MenuList.SelectedIndex = 0;
    }

    /// <summary>Switches to another screen, e.g. from a staff member to their timetable.</summary>
    private void Navigate(AppScreen screen, long? recordId)
    {
        var item = _menu.FirstOrDefault(m => m.Screen == screen);
        if (item is null)
        {
            return;
        }

        _pendingRecordId = recordId;
        if (ReferenceEquals(MenuList.SelectedItem, item))
        {
            Show(item);
        }
        else
        {
            MenuList.SelectedItem = item;
        }
    }

    private void MenuList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MenuList.SelectedItem is NavigationMenuItem item)
        {
            Show(item);
        }
    }

    private void Show(NavigationMenuItem item)
    {
        var recordId = _pendingRecordId;
        _pendingRecordId = null;
        ScreenHost.Content = ScreenFactory.Create(item, _context, recordId);
    }

    private void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow(_services);
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
