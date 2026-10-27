using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class CalendarRepository(ConnectionFactory connections) : ICalendarRepository
{
    public IReadOnlyList<CalendarEvent> EventsInMonth(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT id, event_date, title, description FROM calendar_events " +
            "WHERE event_date >= @from AND event_date < @to ORDER BY event_date, id",
            ("@from", first.ToDbText()), ("@to", first.AddMonths(1).ToDbText()));
        return command.ReadAll(r => new CalendarEvent(r.GetInt64(0), r.GetDate(1), r.GetString(2), r.GetNullableString(3)));
    }

    public IReadOnlyList<CalendarEvent> Upcoming(DateOnly from, int count)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT id, event_date, title, description FROM calendar_events " +
            "WHERE event_date >= @from ORDER BY event_date, id LIMIT @count",
            ("@from", from.ToDbText()), ("@count", count));
        return command.ReadAll(r => new CalendarEvent(r.GetInt64(0), r.GetDate(1), r.GetString(2), r.GetNullableString(3)));
    }

    public long Add(CalendarEvent calendarEvent)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "INSERT INTO calendar_events (event_date, title, description) VALUES (@date, @title, @description) RETURNING id",
            ("@date", calendarEvent.Date.ToDbText()), ("@title", calendarEvent.Title), ("@description", calendarEvent.Description));
        return (long)command.ExecuteScalar()!;
    }

    public bool Update(CalendarEvent calendarEvent)
    {
        ArgumentNullException.ThrowIfNull(calendarEvent);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "UPDATE calendar_events SET event_date = @date, title = @title, description = @description WHERE id = @id",
            ("@date", calendarEvent.Date.ToDbText()), ("@title", calendarEvent.Title),
            ("@description", calendarEvent.Description), ("@id", calendarEvent.Id));
        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(long id)
    {
        using var connection = connections.Open();
        using var command = connection.Sql("DELETE FROM calendar_events WHERE id = @id", ("@id", id));
        return command.ExecuteNonQuery() == 1;
    }
}
