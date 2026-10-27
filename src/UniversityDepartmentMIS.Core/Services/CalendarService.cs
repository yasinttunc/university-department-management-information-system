using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>One square of the month view. Days from the neighbouring months fill the first and last week.</summary>
public sealed record CalendarDay(DateOnly Date, bool IsInMonth, IReadOnlyList<CalendarEvent> Events);

/// <summary>A month laid out in whole weeks, Monday first.</summary>
public sealed record CalendarMonth(int Year, int Month, IReadOnlyList<CalendarDay> Days)
{
    public int Weeks => Days.Count / 7;
}

/// <summary>Builds the month view and checks events before they are saved.</summary>
public sealed class CalendarService(ICalendarRepository calendar)
{
    public const int MaxTitleLength = 100;
    public const int MaxDescriptionLength = 500;

    public CalendarMonth BuildMonth(int year, int month) => Layout(year, month, calendar.EventsInMonth(year, month));

    /// <summary>Lays out the month in Monday-to-Sunday weeks and attaches each event to its day.</summary>
    public static CalendarMonth Layout(int year, int month, IEnumerable<CalendarEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        var first = new DateOnly(year, month, 1);
        var last = first.AddMonths(1).AddDays(-1);

        // DayOfWeek.Sunday is 0; shift so Monday is 0 and Sunday is 6.
        var start = first.AddDays(-(((int)first.DayOfWeek + 6) % 7));
        var end = last.AddDays(6 - (((int)last.DayOfWeek + 6) % 7));

        var byDate = events.GroupBy(e => e.Date).ToDictionary(g => g.Key, g => (IReadOnlyList<CalendarEvent>)g.ToList());
        var days = new List<CalendarDay>();
        for (var date = start; date <= end; date = date.AddDays(1))
        {
            var dayEvents = byDate.TryGetValue(date, out var found) ? found : Array.Empty<CalendarEvent>();
            days.Add(new CalendarDay(date, date.Month == month, dayEvents));
        }

        return new CalendarMonth(year, month, days);
    }

    /// <summary>Adds a new event (Id 0) or updates an existing one.</summary>
    public OperationResult Save(CalendarEvent calendarEvent)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        var cleaned = calendarEvent with
        {
            Title = calendarEvent.Title?.Trim() ?? string.Empty,
            Description = string.IsNullOrWhiteSpace(calendarEvent.Description) ? null : calendarEvent.Description.Trim(),
        };

        var errors = new List<ValidationError>();
        if (cleaned.Title.Length == 0 || cleaned.Title.Length > MaxTitleLength)
        {
            errors.Add(new ValidationError(nameof(CalendarEvent.Title), $"Title is required (up to {MaxTitleLength} characters)."));
        }

        if (cleaned.Description?.Length > MaxDescriptionLength)
        {
            errors.Add(new ValidationError(nameof(CalendarEvent.Description), $"Description can be up to {MaxDescriptionLength} characters."));
        }

        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        if (cleaned.Id == 0)
        {
            return OperationResult.Success(calendar.Add(cleaned));
        }

        return calendar.Update(cleaned)
            ? OperationResult.Success(cleaned.Id)
            : OperationResult.Failure(nameof(CalendarEvent.Id), "This event no longer exists.");
    }

    public bool Delete(long id) => calendar.Delete(id);
}
