using System.Windows;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Desktop.Views.Dialogs;

/// <summary>Adds a calendar event (Id 0) or edits an existing one.</summary>
public partial class EventDialog : Window
{
    private readonly CalendarService _calendar;
    private readonly CalendarEvent _original;

    public EventDialog(CalendarService calendar, CalendarEvent calendarEvent)
    {
        InitializeComponent();
        _calendar = calendar;
        _original = calendarEvent;

        HeaderText.Text = calendarEvent.Id == 0 ? "New event" : "Edit event";
        Title = HeaderText.Text;
        DateInput.SelectedDate = calendarEvent.Date.ToDateTime(TimeOnly.MinValue);
        TitleInput.Text = calendarEvent.Title;
        DescriptionInput.Text = calendarEvent.Description;
        Loaded += (_, _) => TitleInput.Focus();
    }

    /// <summary>The date of the saved event, so the calendar can jump to it.</summary>
    public DateOnly? SavedDate { get; private set; }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DateInput.SelectedDate is not { } date)
        {
            ErrorList.ItemsSource = new[] { "Choose a date." };
            return;
        }

        var updated = _original with
        {
            Date = DateOnly.FromDateTime(date),
            Title = TitleInput.Text,
            Description = DescriptionInput.Text,
        };
        var result = _calendar.Save(updated);
        if (!result.Succeeded)
        {
            ErrorList.ItemsSource = result.Errors.Select(error => error.Message).ToList();
            return;
        }

        SavedDate = updated.Date;
        DialogResult = true;
    }
}
