using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UniversityDepartmentMIS.Core.Navigation;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

public partial class DashboardView : UserControl
{
    private readonly ScreenContext _context;

    public DashboardView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;

        var summary = context.Services.Dashboard.Build(DateOnly.FromDateTime(DateTime.Today));
        var culture = CultureInfo.GetCultureInfo("en-GB");

        StudentsNumber.Text = summary.TotalStudents.ToString(culture);
        var max = Math.Max(1, summary.StudentsByYear.Values.DefaultIfEmpty(0).Max());
        YearBars.ItemsSource = summary.StudentsByYear
            .OrderBy(pair => pair.Key)
            .Select(pair => new YearBar($"Year {pair.Key}", pair.Value, max))
            .ToList();

        OccupancyNumber.Text = $"{summary.AverageOccupancyPercent.ToString("0", culture)}%";
        OccupancyDetail.Text = $"{summary.FullSections} of {summary.Sections} sections full";

        AdvisingNumber.Text = summary.AdviseesPerAdvisor.ToString("0.#", culture);
        AdvisingDetail.Text = $"{summary.Advisors} advisors, {summary.EvaluationsOutstanding} evaluations still to record";

        RemindersList.ItemsSource = summary.NextReminders;
        NoReminders.Visibility = summary.NextReminders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        EventsList.ItemsSource = summary.NextEvents;
        NoEvents.Visibility = summary.NextEvents.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void StudentsCard_Click(object sender, MouseButtonEventArgs e) => _context.Navigate(AppScreen.Students);

    private void CoursesCard_Click(object sender, MouseButtonEventArgs e) => _context.Navigate(AppScreen.Courses);

    private void AdvisingCard_Click(object sender, MouseButtonEventArgs e) => _context.Navigate(AppScreen.Advising);

    private void RemindersButton_Click(object sender, RoutedEventArgs e) => _context.Navigate(AppScreen.Reminders);

    private void CalendarButton_Click(object sender, RoutedEventArgs e) => _context.Navigate(AppScreen.Calendar);

    public sealed record YearBar(string Label, int Count, int Max);
}
