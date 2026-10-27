using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class CourseRepository(ConnectionFactory connections) : ICourseRepository
{
    public IReadOnlyList<Course> ListCourses(int? yearLevel = null)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT id, code, title, credits, ects, year_level FROM courses " +
            "WHERE @level IS NULL OR year_level = @level ORDER BY code",
            ("@level", yearLevel));
        return command.ReadAll(r => new Course(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetNullableInt32(3), r.GetNullableInt32(4), r.GetInt32(5)));
    }

    public IReadOnlyList<SectionWithOccupancy> ListSectionsWithOccupancy(long? courseId = null)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT s.id, c.id, c.code, c.title, s.section_number, s.room, s.capacity, " +
            "(SELECT count(*) FROM enrolments e WHERE e.section_id = s.id), " +
            "s.lecturer_id, st.title, st.first_name, st.last_name " +
            "FROM sections s JOIN courses c ON c.id = s.course_id " +
            "LEFT JOIN staff st ON st.id = s.lecturer_id " +
            "WHERE @course IS NULL OR s.course_id = @course " +
            "ORDER BY c.code, s.section_number",
            ("@course", courseId));
        return command.ReadAll(r => new SectionWithOccupancy(
            r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetNullableString(5),
            r.GetInt32(6), r.GetInt32(7), r.GetNullableInt64(8), r.GetLecturerName(9)));
    }

    public Course? GetCourse(long courseId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT id, code, title, credits, ects, year_level FROM courses WHERE id = @id", ("@id", courseId));
        return command.ReadSingleOrDefault(r => new Course(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetNullableInt32(3), r.GetNullableInt32(4), r.GetInt32(5)));
    }

    public Section? GetSection(long sectionId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT id, course_id, section_number, room, capacity, lecturer_id FROM sections WHERE id = @id",
            ("@id", sectionId));
        return command.ReadSingleOrDefault(r => new Section(
            r.GetInt64(0), r.GetInt64(1), r.GetString(2), r.GetNullableString(3), r.GetInt32(4), r.GetNullableInt64(5)));
    }
}
