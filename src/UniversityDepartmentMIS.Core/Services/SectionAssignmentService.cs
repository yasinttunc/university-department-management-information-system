using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Result of filling a section: how many places were filled and from how large a pool.</summary>
public sealed record AssignmentResult(long SectionId, int Capacity, int Assigned, int AssignedFromCourseYear, int CandidatePool);

/// <summary>
/// Fills a section with randomly chosen students, replacing its current class list.
/// Students in the course's year of study are chosen first, then students from other years; nobody is
/// placed in two sections of the same course. Passing a seed makes the choice repeatable.
/// </summary>
public sealed class SectionAssignmentService(
    ICourseRepository courses, IStudentRepository students, IEnrolmentRepository enrolments)
{
    public AssignmentResult Assign(long sectionId, int? seed = null)
    {
        var section = courses.GetSection(sectionId)
            ?? throw new ArgumentException($"Section {sectionId} does not exist.", nameof(sectionId));
        var course = courses.GetCourse(section.CourseId)
            ?? throw new InvalidOperationException($"Course {section.CourseId} does not exist.");

        var taken = enrolments.StudentsInOtherSectionsOfCourse(sectionId);
        var candidates = students.List().Where(s => !taken.Contains(s.Id)).ToList();
        var random = seed is { } value ? new Random(value) : Random.Shared;

        var sameYear = candidates.Where(s => s.YearOfStudy == course.YearLevel).ToList();
        var otherYears = candidates.Where(s => s.YearOfStudy != course.YearLevel).ToList();
        Shuffle(sameYear, random);
        Shuffle(otherYears, random);

        var chosen = sameYear.Concat(otherYears).Take(section.Capacity).ToList();
        enrolments.ReplaceSectionEnrolments(sectionId, chosen.Select(s => s.Id));

        return new AssignmentResult(
            sectionId, section.Capacity, chosen.Count, chosen.Count(s => s.YearOfStudy == course.YearLevel), candidates.Count);
    }

    /// <summary>Fisher-Yates shuffle in place: every order is equally likely.</summary>
    public static void Shuffle<T>(IList<T> items, Random random)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(random);
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
