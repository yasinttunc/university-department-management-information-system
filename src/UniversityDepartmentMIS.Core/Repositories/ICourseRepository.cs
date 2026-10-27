using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface ICourseRepository
{
    /// <summary>Lists courses by code, optionally only one year level.</summary>
    IReadOnlyList<Course> ListCourses(int? yearLevel = null);

    /// <summary>Lists sections with enrolment counts, optionally for one course only.</summary>
    IReadOnlyList<SectionWithOccupancy> ListSectionsWithOccupancy(long? courseId = null);

    Course? GetCourse(long courseId);

    Section? GetSection(long sectionId);
}
