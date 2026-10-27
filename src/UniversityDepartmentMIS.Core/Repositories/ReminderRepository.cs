using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class ReminderRepository(ConnectionFactory connections) : IReminderRepository
{
    public IReadOnlyList<Reminder> ListUpcoming(DateOnly from)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT id, remind_on, note FROM reminders WHERE remind_on >= @from ORDER BY remind_on, id",
            ("@from", from.ToDbText()));
        return command.ReadAll(r => new Reminder(r.GetInt64(0), r.GetDate(1), r.GetString(2)));
    }

    public long Add(Reminder reminder)
    {
        ArgumentNullException.ThrowIfNull(reminder);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "INSERT INTO reminders (remind_on, note) VALUES (@date, @note) RETURNING id",
            ("@date", reminder.RemindOn.ToDbText()), ("@note", reminder.Note));
        return (long)command.ExecuteScalar()!;
    }

    public bool Delete(long id)
    {
        using var connection = connections.Open();
        using var command = connection.Sql("DELETE FROM reminders WHERE id = @id", ("@id", id));
        return command.ExecuteNonQuery() == 1;
    }
}
