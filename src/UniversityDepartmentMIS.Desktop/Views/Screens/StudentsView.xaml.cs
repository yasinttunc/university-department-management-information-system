using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Desktop.Navigation;
using UniversityDepartmentMIS.Desktop.Views.Dialogs;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

public partial class StudentsView : UserControl
{
    private readonly ScreenContext _context;
    private readonly bool _ready;

    public StudentsView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;

        YearFilter.ItemsSource = new[] { "All years", "Year 1", "Year 2", "Year 3", "Year 4" };
        YearFilter.SelectedIndex = 0;
        AddButton.Visibility = Permissions.CanEditStudents(context.User.Role) ? Visibility.Visible : Visibility.Collapsed;

        _ready = true;
        Refresh();
    }

    private void Refresh()
    {
        if (!_ready)
        {
            return;
        }

        int? year = YearFilter.SelectedIndex > 0 ? YearFilter.SelectedIndex : null;
        var students = _context.Services.Students.Browse(year, SearchBox.Text);
        StudentsGrid.ItemsSource = students;
        CountText.Text = students.Count == 1 ? "1 student" : $"{students.Count} students";
    }

    private void Filter_Changed(object sender, RoutedEventArgs e) => Refresh();

    private void AddButton_Click(object sender, RoutedEventArgs e) => OpenDialog(null);

    private void StudentsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e) => OpenSelected();

    private void StudentsGrid_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OpenSelected();
            e.Handled = true;
        }
    }

    private void OpenSelected()
    {
        if (StudentsGrid.SelectedItem is StudentListItem item)
        {
            OpenDialog(item.Id);
        }
    }

    private void OpenDialog(long? studentId)
    {
        var dialog = new StudentDialog(_context, studentId) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true)
        {
            Refresh();
        }
    }
}
