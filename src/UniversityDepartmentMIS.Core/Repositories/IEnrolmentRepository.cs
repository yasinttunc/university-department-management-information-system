using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface IEnrolmentRepository
{
    /// <summary>Students enrolled in a section, by student number.</summary>
    IReadOnlyList<Student> StudentsInSection(long sectionId);

    /// <summary>
    /// Replaces the whole class list of a section in one transaction. If any student id is invalid,
    /// nothing changes.
    /// </summary>
    /// <summary>Ids of students enrolled in any other section of the same course as this section.</summary>
    IReadOnlySet<long> StudentsInOtherSectionsOfCourse(long sectionId);

    void ReplaceSectionEnrolments(long sectionId, IEnumerable<long> studentIds);
}
