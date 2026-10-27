using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Services;
using UniversityDepartmentMIS.Desktop.Navigation;
using UniversityDepartmentMIS.Desktop.Views.Dialogs;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

/// <summary>Month view (Monday first) built from <see cref="CalendarMonth"/>, with the selected day's events beside it.</summary>
public partial class CalendarView : UserControl
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("en-GB");

    private readonly ScreenContext _context;
    private DateOnly _month;
    private DateOnly _selected;
    private CalendarMonth? _shown;

    public CalendarView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;

        foreach (var day in new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" })
        {
            DayNames.Children.Add(new TextBlock { Text = day, HorizontalAlignment = HorizontalAlignment.Center, Opacity = 0.7 });
        }

        _selected = DateOnly.FromDateTime(DateTime.Today);
        _month = new DateOnly(_selected.Year, _selected.Month, 1);
        Render();
    }

    private CalendarEvent? SelectedEvent => EventsList.SelectedItem as CalendarEvent;

    private void Render()
    {
        _shown = _context.Services.CalendarService.BuildMonth(_month.Year, _month.Month);
        MonthText.Text = _month.ToString("MMMM yyyy", Culture);

        DaysGrid.Children.Clear();
        DaysGrid.Rows = _shown.Weeks;
        foreach (var day in _shown.Days)
        {
            DaysGrid.Children.Add(DayButton(day));
        }

        ShowSelectedDay();
    }

    private Button DayButton(CalendarDay day)
    {
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = day.Date.Day.ToString(Culture), FontWeight = FontWeights.SemiBold });
        if (day.Events.Count > 0)
        {
            text.Children.Add(new TextBlock
            {
                Text = day.Events.Count == 1 ? day.Events[0].Title : $"{day.Events.Count} events",
                FontSize = 11,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
        }

        var button = new Button
        {
            Content = text,
            Height = double.NaN,
            MinHeight = 56,
            Margin = new Thickness(2),
            Padding = new Thickness(6, 4, 6, 4),
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Top,
            Opacity = day.IsInMonth ? 1 : 0.45,
            Tag = day.Date,
        };

        if (day.Date == _selected)
        {
            button.SetResourceReference(StyleProperty, "MaterialDesignRaisedButton");
        }
        else
        {
            button.SetResourceReference(StyleProperty, "MaterialDesignOutlinedButton");
            button.SetResourceReference(BorderBrushProperty, "AppDividerBrush");
            if (day.Events.Count > 0)
            {
                button.SetResourceReference(BackgroundProperty, "AppPrimaryLightBrush");
                button.SetResourceReference(ForegroundProperty, "AppPrimaryLightForegroundBrush");
            }
            else
            {
                button.SetResourceReference(ForegroundProperty, "AppForegroundBrush");
            }
        }

        AutomationProperties.SetName(button, day.Date.ToString("d MMMM yyyy", Culture) + (day.Events.Count > 0 ? $", {day.Events.Count} event(s)" : string.Empty));
        button.Click += (_, _) => Select(day.Date);
        button.MouseDoubleClick += (_, _) => AddEvent(day.Date);
        return button;
    }

    private void Select(DateOnly date)
    {
        _selected = date;
        if (date.Year != _month.Year || date.Month != _month.Month)
        {
            _month = new DateOnly(date.Year, date.Month, 1);
        }

        Render();
    }

    private void ShowSelectedDay()
    {
        SelectedDayText.Text = _selected.ToString("dddd d MMMM yyyy", Culture);
        var events = _shown?.Days.FirstOrDefault(d => d.Date == _selected)?.Events ?? Array.Empty<CalendarEvent>();
        EventsList.ItemsSource = events;
        EditButton.IsEnabled = false;
        DeleteButton.IsEnabled = false;
    }

    private void PreviousButton_Click(object sender, RoutedEventArgs e) => Select(_month.AddMonths(-1));

    private void NextButton_Click(object sender, RoutedEventArgs e) => Select(_month.AddMonths(1));

    private void EventsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditButton.IsEnabled = SelectedEvent is not null;
        DeleteButton.IsEnabled = SelectedEvent is not null;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => AddEvent(_selected);

    private void AddEvent(DateOnly date) => OpenDialog(new CalendarEvent(0, date, string.Empty, null));

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEvent is { } selected)
        {
            OpenDialog(selected);
        }
    }

    private void OpenDialog(CalendarEvent calendarEvent)
    {
        var dialog = new EventDialog(_context.Services.CalendarService, calendarEvent) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() == true && dialog.SavedDate is { } saved)
        {
            Select(saved);
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedEvent is not { } selected)
        {
            return;
        }

        var answer = MessageBox.Show(Window.GetWindow(this)!, $"Delete \"{selected.Title}\"?", "Delete event",
            MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
        if (answer == MessageBoxResult.Yes)
        {
            _context.Services.CalendarService.Delete(selected.Id);
            Render();
        }
    }
}
