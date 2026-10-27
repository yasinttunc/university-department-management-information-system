using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class TimetableRepository(ConnectionFactory connections) : ITimetableRepository
{
    private const string Select =
        "SELECT t.section_id, t.day_of_week, t.start_time, t.end_time, c.code, c.title, s.section_number, s.room, " +
        "s.lecturer_id, st.title, st.first_name, st.last_name " +
        "FROM timetable_slots t JOIN sections s ON s.id = t.section_id " +
        "JOIN courses c ON c.id = s.course_id LEFT JOIN staff st ON st.id = s.lecturer_id ";

    private const string Order = " ORDER BY t.day_of_week, t.start_time, c.code, s.section_number";

    public IReadOnlyList<TimetableEntry> ForLecturer(long staffId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + "WHERE s.lecturer_id = @staff" + Order, ("@staff", staffId));
        return command.ReadAll(Map);
    }

    public IReadOnlyList<TimetableEntry> ForSection(long sectionId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + "WHERE t.section_id = @section" + Order, ("@section", sectionId));
        return command.ReadAll(Map);
    }

    private static TimetableEntry Map(SqliteDataReader r) => new(
        r.GetInt64(0), (DayOfWeek)r.GetInt32(1), r.GetTime(2), r.GetTime(3), r.GetString(4), r.GetString(5),
        r.GetString(6), r.GetNullableString(7), r.GetNullableInt64(8), r.GetLecturerName(9));
}
