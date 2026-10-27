using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class SectionAssignmentServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly CourseRepository _courses;
    private readonly EnrolmentRepository _enrolments;
    private readonly SectionAssignmentService _service;

    public SectionAssignmentServiceTests()
    {
        _courses = new CourseRepository(_db.Connections);
        _enrolments = new EnrolmentRepository(_db.Connections);
        _service = new SectionAssignmentService(_courses, new StudentRepository(_db.Connections), _enrolments);
    }

    public void Dispose() => _db.Dispose();

    private SectionRef Section(string code, string number)
    {
        var s = _courses.ListSectionsWithOccupancy().Single(x => x.CourseCode == code && x.SectionNumber == number);
        return new SectionRef(s.SectionId, s.CourseId, s.Capacity);
    }

    private sealed record SectionRef(long Id, long CourseId, int Capacity);

    [Fact]
    public void Assign_FillsTheSectionUpToItsCapacity()
    {
        var section = Section("TKM435", "41");

        var result = _service.Assign(section.Id, seed: 1);

        Assert.Equal(section.Capacity, result.Assigned);
        Assert.Equal(section.Capacity, _enrolments.StudentsInSection(section.Id).Count);
    }

    [Fact]
    public void Assign_PrefersStudentsInTheCoursesYear()
    {
        // TKM435 is a year 4 course with 40 places; the demo data has 17 year 4 students.
        var section = Section("TKM435", "41");

        var result = _service.Assign(section.Id, seed: 1);
        var students = _enrolments.StudentsInSection(section.Id);

        Assert.Equal(17, result.AssignedFromCourseYear);
        Assert.Equal(17, students.Count(s => s.YearOfStudy == 4));
    }

    [Fact]
    public void Assign_NeverPutsAStudentInTwoSectionsOfTheSameCourse()
    {
        var course = _courses.ListSectionsWithOccupancy().Where(s => s.CourseCode == "TKM108").ToList();

        foreach (var section in course)
        {
            _service.Assign(section.SectionId, seed: 7);
        }

        var all = course.SelectMany(s => _enrolments.StudentsInSection(s.SectionId)).Select(s => s.Id).ToList();
        Assert.Equal(all.Count, all.Distinct().Count());
    }

    [Fact]
    public void Assign_WithTheSameSeed_IsRepeatable()
    {
        var section = Section("TKM405", "41");

        _service.Assign(section.Id, seed: 42);
        var first = _enrolments.StudentsInSection(section.Id).Select(s => s.Id).ToList();
        _service.Assign(section.Id, seed: 42);

        Assert.Equal(first, _enrolments.StudentsInSection(section.Id).Select(s => s.Id));
    }

    [Fact]
    public void Assign_SectionWithNoCapacity_EndsUpEmpty()
    {
        var empty = _courses.ListSectionsWithOccupancy().Single(s => s.Capacity == 0);

        var result = _service.Assign(empty.SectionId, seed: 1);

        Assert.Equal(0, result.Assigned);
        Assert.Empty(_enrolments.StudentsInSection(empty.SectionId));
    }

    [Fact]
    public void Assign_UnknownSection_Throws()
    {
        Assert.Throws<ArgumentException>(() => _service.Assign(999));
    }

    [Fact]
    public void Shuffle_ReturnsAPermutation()
    {
        var items = Enumerable.Range(1, 50).ToList();

        SectionAssignmentService.Shuffle(items, new Random(3));

        Assert.Equal(Enumerable.Range(1, 50), items.Order());
        Assert.NotEqual(Enumerable.Range(1, 50), items);
    }
}
