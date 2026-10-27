using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface ICalendarRepository
{
    /// <summary>Events in the given month, by date.</summary>
    IReadOnlyList<CalendarEvent> EventsInMonth(int year, int month);

    /// <summary>The next events on or after the given date, soonest first.</summary>
    IReadOnlyList<CalendarEvent> Upcoming(DateOnly from, int count);

    long Add(CalendarEvent calendarEvent);

    bool Update(CalendarEvent calendarEvent);

    bool Delete(long id);
}
