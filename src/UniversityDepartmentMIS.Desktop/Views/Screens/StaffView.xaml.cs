using System.Windows;
using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Navigation;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

public partial class StaffView : UserControl
{
    private readonly ScreenContext _context;

    public StaffView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;

        var staff = context.Services.Staff.ListWorkloads();
        StaffGrid.ItemsSource = staff;
        SummaryText.Text =
            $"{staff.Count} staff, {staff.Sum(s => s.WeeklyTeachingHours)} teaching hours a week, " +
            $"{staff.Sum(s => s.Advisees)} advisees.";
    }

    private StaffWorkload? Selected => StaffGrid.SelectedItem as StaffWorkload;

    private void StaffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        TimetableButton.IsEnabled = Selected is not null;
        AdviseesButton.IsEnabled = Selected is not null;
    }

    private void TimetableButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { } selected)
        {
            _context.Navigate(AppScreen.Timetable, selected.Staff.Id);
        }
    }

    private void AdviseesButton_Click(object sender, RoutedEventArgs e)
    {
        if (Selected is { } selected)
        {
            _context.Navigate(AppScreen.Advising, selected.Staff.Id);
        }
    }
}
