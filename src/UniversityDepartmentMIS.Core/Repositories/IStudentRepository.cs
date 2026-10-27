using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface IStudentRepository
{
    /// <summary>Lists students by student number, optionally only one year of study.</summary>
    IReadOnlyList<Student> List(int? yearOfStudy = null);

    /// <summary>Matches the text against name, student number and email. Wildcards are treated literally.</summary>
    IReadOnlyList<Student> Search(string text);

    /// <summary>
    /// The student list screen: optional year filter and search text (name, number or email), with each
    /// student's advisor.
    /// </summary>
    IReadOnlyList<StudentListItem> Browse(int? yearOfStudy, string? search);

    /// <summary>
    /// Names of the unique fields (StudentNumber, NationalId, Email) that another student already uses.
    /// The student's own record (same <see cref="Student.Id"/>) is ignored.
    /// </summary>
    IReadOnlyList<string> FindDuplicateFields(Student student);

    Student? Get(long id);

    /// <summary>Inserts the student (its <see cref="Student.Id"/> is ignored) and returns the new id.</summary>
    long Add(Student student);

    /// <summary>Inserts several students in one transaction: either all are saved or none.</summary>
    int AddRange(IReadOnlyList<Student> newStudents);

    bool Update(Student student);

    /// <summary>Deletes the student together with their enrolments and advisor assignment.</summary>
    bool Delete(long id);
}
