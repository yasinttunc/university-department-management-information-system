using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class EnrolmentRepository(ConnectionFactory connections) : IEnrolmentRepository
{
    public IReadOnlyList<Student> StudentsInSection(long sectionId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT s.id, s.student_number, s.first_name, s.last_name, s.national_id, s.email, s.phone, s.address, " +
            "s.programme, s.year_of_study, s.enrolment_year FROM enrolments e JOIN students s ON s.id = e.student_id " +
            "WHERE e.section_id = @section ORDER BY s.student_number",
            ("@section", sectionId));
        return command.ReadAll(r => new Student(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
            r.GetNullableString(6), r.GetNullableString(7), r.GetString(8), r.GetInt32(9), r.GetInt32(10)));
    }

    public IReadOnlySet<long> StudentsInOtherSectionsOfCourse(long sectionId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT DISTINCT e.student_id FROM enrolments e JOIN sections other ON other.id = e.section_id " +
            "JOIN sections target ON target.id = @section " +
            "WHERE other.course_id = target.course_id AND other.id <> target.id",
            ("@section", sectionId));
        return command.ReadAll(r => r.GetInt64(0)).ToHashSet();
    }

    public void ReplaceSectionEnrolments(long sectionId, IEnumerable<long> studentIds)
    {
        ArgumentNullException.ThrowIfNull(studentIds);
        var ids = studentIds.Distinct().ToList();

        using var connection = connections.Open();
        using var transaction = connection.BeginTransaction();

        using (var delete = connection.Sql("DELETE FROM enrolments WHERE section_id = @section", ("@section", sectionId)))
        {
            delete.Transaction = transaction;
            delete.ExecuteNonQuery();
        }

        using (var insert = connection.Sql(
            "INSERT INTO enrolments (section_id, student_id) VALUES (@section, @student)",
            ("@section", sectionId), ("@student", 0L)))
        {
            insert.Transaction = transaction;
            foreach (var id in ids)
            {
                insert.Parameters["@student"].Value = id;
                insert.ExecuteNonQuery();
            }
        }

        transaction.Commit();
    }
}
