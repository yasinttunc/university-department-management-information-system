using System.Windows;
using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

/// <summary>Upcoming reminders, soonest first. Past reminders are not shown.</summary>
public partial class RemindersView : UserControl
{
    private readonly ScreenContext _context;

    public RemindersView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;
        DateInput.SelectedDate = DateTime.Today;
        Refresh();
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

    private void Refresh()
    {
        var reminders = _context.Services.ReminderService.Upcoming(Today);
        RemindersGrid.ItemsSource = reminders;
        CountText.Text = reminders.Count == 1 ? "1 upcoming reminder" : $"{reminders.Count} upcoming reminders";
        DeleteButton.IsEnabled = false;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (DateInput.SelectedDate is not { } date)
        {
            ShowError("Choose a date.");
            return;
        }

        var result = _context.Services.ReminderService.Add(DateOnly.FromDateTime(date), NoteInput.Text);
        if (!result.Succeeded)
        {
            ShowError(result.Errors[0].Message);
            return;
        }

        ErrorText.Visibility = Visibility.Collapsed;
        NoteInput.Clear();
        Refresh();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }

    private void RemindersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        DeleteButton.IsEnabled = RemindersGrid.SelectedItem is Reminder;

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (RemindersGrid.SelectedItem is Reminder reminder)
        {
            _context.Services.ReminderService.Delete(reminder.Id);
            Refresh();
        }
    }
}
