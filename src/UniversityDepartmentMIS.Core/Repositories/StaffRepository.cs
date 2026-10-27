using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class StaffRepository(ConnectionFactory connections) : IStaffRepository
{
    private const string Select = "SELECT id, title, first_name, last_name, email FROM staff";

    public IReadOnlyList<StaffMember> List()
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + " ORDER BY last_name, first_name");
        return command.ReadAll(Map);
    }

    public StaffMember? Get(long id)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + " WHERE id = @id", ("@id", id));
        return command.ReadSingleOrDefault(Map);
    }

    public IReadOnlyList<StaffWorkload> ListWorkloads()
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT st.id, st.title, st.first_name, st.last_name, st.email, " +
            "(SELECT count(*) FROM sections s WHERE s.lecturer_id = st.id), " +
            "(SELECT count(*) FROM timetable_slots t JOIN sections s ON s.id = t.section_id WHERE s.lecturer_id = st.id), " +
            "(SELECT count(*) FROM advisor_assignments a WHERE a.advisor_id = st.id) " +
            "FROM staff st ORDER BY st.last_name, st.first_name");
        return command.ReadAll(r => new StaffWorkload(Map(r), r.GetInt32(5), r.GetInt32(6), r.GetInt32(7)));
    }

    private static StaffMember Map(SqliteDataReader r) => new(
        r.GetInt64(0), StaffTitles.Parse(r.GetString(1)), r.GetString(2), r.GetString(3), r.GetString(4));
}
