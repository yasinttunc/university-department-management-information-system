using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class StudentRepository(ConnectionFactory connections) : IStudentRepository
{
    private const string Select =
        "SELECT id, student_number, first_name, last_name, national_id, email, phone, address, " +
        "programme, year_of_study, enrolment_year FROM students";

    public IReadOnlyList<Student> List(int? yearOfStudy = null)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            Select + " WHERE @year IS NULL OR year_of_study = @year ORDER BY student_number", ("@year", yearOfStudy));
        return command.ReadAll(Map);
    }

    public IReadOnlyList<Student> Search(string text)
    {
        var term = (text ?? string.Empty).Trim();
        if (term.Length == 0)
        {
            return List();
        }

        using var connection = connections.Open();
        using var command = connection.Sql(
            Select + " WHERE (first_name || ' ' || last_name) LIKE '%' || @term || '%' ESCAPE '\\'" +
            " OR student_number LIKE '%' || @term || '%' ESCAPE '\\'" +
            " OR email LIKE '%' || @term || '%' ESCAPE '\\'" +
            " ORDER BY student_number",
            ("@term", SqliteExtensions.EscapeLike(term)));
        return command.ReadAll(Map);
    }

    public IReadOnlyList<StudentListItem> Browse(int? yearOfStudy, string? search)
    {
        var term = (search ?? string.Empty).Trim();
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT s.id, s.student_number, s.first_name, s.last_name, s.year_of_study, s.email, a.advisor_id, " +
            "st.title, st.first_name, st.last_name " +
            "FROM students s LEFT JOIN advisor_assignments a ON a.student_id = s.id " +
            "LEFT JOIN staff st ON st.id = a.advisor_id " +
            "WHERE (@year IS NULL OR s.year_of_study = @year) AND (@term = '' " +
            "OR (s.first_name || ' ' || s.last_name) LIKE '%' || @term || '%' ESCAPE '\\' " +
            "OR s.student_number LIKE '%' || @term || '%' ESCAPE '\\' " +
            "OR s.email LIKE '%' || @term || '%' ESCAPE '\\') " +
            "ORDER BY s.student_number",
            ("@year", yearOfStudy), ("@term", SqliteExtensions.EscapeLike(term)));
        return command.ReadAll(r => new StudentListItem(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4), r.GetString(5),
            r.GetNullableInt64(6), r.GetLecturerName(7)));
    }

    public IReadOnlyList<string> FindDuplicateFields(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT max(student_number = @number), max(national_id = @nationalId), max(email = @email COLLATE NOCASE) " +
            "FROM students WHERE id <> @id",
            ("@number", student.StudentNumber), ("@nationalId", student.NationalId), ("@email", student.Email),
            ("@id", student.Id));
        using var reader = command.ExecuteReader();
        var fields = new List<string>();
        if (reader.Read())
        {
            if (reader.GetNullableInt64(0) == 1)
            {
                fields.Add(nameof(Student.StudentNumber));
            }

            if (reader.GetNullableInt64(1) == 1)
            {
                fields.Add(nameof(Student.NationalId));
            }

            if (reader.GetNullableInt64(2) == 1)
            {
                fields.Add(nameof(Student.Email));
            }
        }

        return fields;
    }

    public Student? Get(long id)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + " WHERE id = @id", ("@id", id));
        return command.ReadSingleOrDefault(Map);
    }

    public long Add(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "INSERT INTO students (student_number, first_name, last_name, national_id, email, phone, address, " +
            "programme, year_of_study, enrolment_year) VALUES (@number, @first, @last, @nationalId, @email, " +
            "@phone, @address, @programme, @year, @enrolmentYear) RETURNING id",
            Parameters(student));
        return (long)command.ExecuteScalar()!;
    }

    public int AddRange(IReadOnlyList<Student> newStudents)
    {
        ArgumentNullException.ThrowIfNull(newStudents);
        using var connection = connections.Open();
        using var transaction = connection.BeginTransaction();
        foreach (var student in newStudents)
        {
            using var command = connection.Sql(
                "INSERT INTO students (student_number, first_name, last_name, national_id, email, phone, address, " +
                "programme, year_of_study, enrolment_year) VALUES (@number, @first, @last, @nationalId, @email, " +
                "@phone, @address, @programme, @year, @enrolmentYear)",
                Parameters(student));
            command.Transaction = transaction;
            command.ExecuteNonQuery();
        }

        transaction.Commit();
        return newStudents.Count;
    }

    public bool Update(Student student)
    {
        ArgumentNullException.ThrowIfNull(student);
        using var connection = connections.Open();
        using var command = connection.Sql(
            "UPDATE students SET student_number = @number, first_name = @first, last_name = @last, " +
            "national_id = @nationalId, email = @email, phone = @phone, address = @address, " +
            "programme = @programme, year_of_study = @year, enrolment_year = @enrolmentYear WHERE id = @id",
            [.. Parameters(student), ("@id", student.Id)]);
        return command.ExecuteNonQuery() == 1;
    }

    public bool Delete(long id)
    {
        using var connection = connections.Open();
        using var command = connection.Sql("DELETE FROM students WHERE id = @id", ("@id", id));
        return command.ExecuteNonQuery() == 1;
    }

    private static (string, object?)[] Parameters(Student s) =>
    [
        ("@number", s.StudentNumber), ("@first", s.FirstName), ("@last", s.LastName),
        ("@nationalId", s.NationalId), ("@email", s.Email), ("@phone", s.Phone), ("@address", s.Address),
        ("@programme", s.Programme), ("@year", s.YearOfStudy), ("@enrolmentYear", s.EnrolmentYear),
    ];

    private static Student Map(SqliteDataReader r) => new(
        r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4), r.GetString(5),
        r.GetNullableString(6), r.GetNullableString(7), r.GetString(8), r.GetInt32(9), r.GetInt32(10));
}
