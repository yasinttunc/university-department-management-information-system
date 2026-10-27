using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Services;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

/// <summary>A lecturer's week as a Monday-to-Friday, 09:00-18:00 grid built from <see cref="WeekTimetable"/>.</summary>
public partial class TimetableView : UserControl
{
    private static readonly Brush ClashBackground = new SolidColorBrush(Color.FromRgb(0xFB, 0xE3, 0xE4));
    private static readonly Brush ClashForeground = new SolidColorBrush(Color.FromRgb(0xB0, 0x00, 0x20));

    private readonly ScreenContext _context;

    public TimetableView(ScreenContext context, long? staffId)
    {
        InitializeComponent();
        _context = context;

        var staff = context.Services.Staff.List();
        StaffPicker.ItemsSource = staff;
        StaffPicker.SelectedItem = staff.FirstOrDefault(s => s.Id == staffId) ?? staff.FirstOrDefault();
    }

    private void StaffPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StaffPicker.SelectedItem is StaffMember member)
        {
            Render(_context.Services.TimetableService.ForLecturer(member.Id));
        }
    }

    private void Render(WeekTimetable week)
    {
        WeekGrid.Children.Clear();
        WeekGrid.RowDefinitions.Clear();
        WeekGrid.ColumnDefinitions.Clear();

        WeekGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        foreach (var _ in week.Days)
        {
            WeekGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }

        WeekGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        foreach (var _ in week.SlotStarts)
        {
            WeekGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        }

        for (var d = 0; d < week.Days.Count; d++)
        {
            Place(Header(CultureInfo.GetCultureInfo("en-GB").DateTimeFormat.GetDayName(week.Days[d])), 0, d + 1);
        }

        for (var h = 0; h < week.SlotStarts.Count; h++)
        {
            var start = week.SlotStarts[h];
            Place(Header($"{Hm(start)} - {Hm(start.AddHours(1))}"), h + 1, 0);

            for (var d = 0; d < week.Days.Count; d++)
            {
                Place(Cell(week[week.Days[d], start]), h + 1, d + 1);
            }
        }

        var clashes = week.Clashes.Count;
        SummaryText.Text = $"{week.TeachingHours} teaching hours a week" +
            (clashes == 0 ? ", no clashes" : $", {clashes} clash{(clashes == 1 ? string.Empty : "es")}");

        OutsideText.Visibility = week.OutsideGrid.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        OutsideText.Text = "Outside 09:00-18:00: " + string.Join(", ",
            week.OutsideGrid.Select(e => $"{e.Label} {e.Day} {Hm(e.StartTime)}"));
    }

    private static string Hm(TimeOnly time) => time.ToString("HH:mm", CultureInfo.InvariantCulture);

    private void Place(UIElement element, int row, int column)
    {
        Grid.SetRow(element, row);
        Grid.SetColumn(element, column);
        WeekGrid.Children.Add(element);
    }

    private static TextBlock Header(string text) => new()
    {
        Text = text,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(6, 4, 6, 6),
        VerticalAlignment = VerticalAlignment.Center,
    };

    private static Border Cell(TimetableCell cell)
    {
        var border = new Border
        {
            BorderThickness = new Thickness(0.5),
            Margin = new Thickness(1),
            Padding = new Thickness(6, 2, 6, 2),
            CornerRadius = new CornerRadius(3),
        };
        border.SetResourceReference(Border.BorderBrushProperty, "AppDividerBrush");

        if (cell.IsFree)
        {
            return border;
        }

        var text = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        foreach (var entry in cell.Entries)
        {
            if (text.Inlines.Count > 0)
            {
                text.Inlines.Add(new LineBreak());
            }

            text.Inlines.Add(new Run(entry.Label) { FontWeight = FontWeights.SemiBold });
            if (entry.Room is { } room)
            {
                text.Inlines.Add(new Run($"  {room}"));
            }
        }

        if (cell.HasClash)
        {
            border.Background = ClashBackground;
            text.Foreground = ClashForeground;
            border.ToolTip = "Clash: more than one section in this hour";
        }
        else
        {
            border.SetResourceReference(Border.BackgroundProperty, "AppPrimaryLightBrush");
            text.SetResourceReference(TextBlock.ForegroundProperty, "AppPrimaryLightForegroundBrush");
            border.ToolTip = cell.Entries[0].CourseTitle;
        }

        var labels = string.Join(", ", cell.Entries.Select(entry => entry.Label));
        AutomationProperties.SetName(border, $"{cell.Day} {Hm(cell.Start)}: {labels}");
        border.Child = text;
        return border;
    }
}
