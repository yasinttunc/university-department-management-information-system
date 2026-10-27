using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Tests;

public class StaffAndCourseRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Staff_List_ReturnsEveryMemberWithParsedTitles()
    {
        var staff = new StaffRepository(_db.Connections).List();

        Assert.Equal(13, staff.Count);
        Assert.Equal("Akdağ", staff[0].LastName);
        Assert.Contains(staff, s => s.Title == StaffTitle.Professor);
        Assert.Contains(staff, s => s.Title == StaffTitle.Lecturer);
    }

    [Fact]
    public void Staff_Get_BuildsDisplayName()
    {
        var member = new StaffRepository(_db.Connections).Get(1);

        Assert.NotNull(member);
        Assert.Equal("Assoc. Prof. Derya Karaca", member.DisplayName);
        Assert.Null(new StaffRepository(_db.Connections).Get(999));
    }

    [Theory]
    [InlineData(StaffTitle.Professor)]
    [InlineData(StaffTitle.AssociateProfessor)]
    [InlineData(StaffTitle.AssistantProfessor)]
    [InlineData(StaffTitle.Doctor)]
    [InlineData(StaffTitle.Lecturer)]
    public void StaffTitles_RoundTripThroughTheStoredForm(StaffTitle title)
    {
        Assert.Equal(title, StaffTitles.Parse(title.ToAbbreviation()));
    }

    [Fact]
    public void Courses_ListAndFilterByYearLevel()
    {
        var courses = new CourseRepository(_db.Connections);

        Assert.Equal(21, courses.ListCourses().Count);
        Assert.Equal(10, courses.ListCourses(4).Count);
        Assert.Contains(courses.ListCourses(), c => c.Code == "TKM401" && c.Credits is null);
    }

    [Fact]
    public void Sections_WithOccupancy_CountEveryEnrolment()
    {
        var sections = new CourseRepository(_db.Connections).ListSectionsWithOccupancy();

        Assert.Equal(34, sections.Count);
        Assert.Equal(542, sections.Sum(s => s.Enrolled));
        Assert.All(sections, s => Assert.InRange(s.OccupancyPercent, 0, 100));
    }

    [Fact]
    public void Sections_WithoutLecturer_HaveNoLecturerName()
    {
        var section = Assert.Single(
            new CourseRepository(_db.Connections).ListSectionsWithOccupancy(), s => s.LecturerId is null);

        Assert.Null(section.LecturerName);
        Assert.Equal("TKM214-21", section.Label);
    }

    [Fact]
    public void Sections_FilterByCourse_AndGetSection()
    {
        var courses = new CourseRepository(_db.Connections);
        var first = courses.ListSectionsWithOccupancy()[0];

        Assert.All(courses.ListSectionsWithOccupancy(first.CourseId), s => Assert.Equal(first.CourseId, s.CourseId));
        Assert.Equal(first.SectionNumber, courses.GetSection(first.SectionId)!.SectionNumber);
        Assert.Null(courses.GetSection(999));
    }

    [Fact]
    public void Occupancy_WithZeroCapacity_IsZeroPercent()
    {
        var empty = new SectionWithOccupancy(1, 1, "TKM000", "Test", "11", null, 0, 0, null, null);

        Assert.Equal(0, empty.OccupancyPercent);
        Assert.Equal(0, empty.FreePlaces);
    }

    [Fact]
    public void Timetable_ForLecturer_IsSortedAndOnlyTheirSections()
    {
        var entries = new TimetableRepository(_db.Connections).ForLecturer(7);

        Assert.Equal(12, entries.Count);
        Assert.All(entries, e => Assert.True(e.LecturerId == 7));
        Assert.All(entries, e => Assert.InRange(e.Day, DayOfWeek.Monday, DayOfWeek.Friday));
        Assert.Equal(entries.OrderBy(e => e.Day).ThenBy(e => e.StartTime), entries);
    }

    [Fact]
    public void Timetable_ForSection_ReturnsItsSlots()
    {
        var entries = new TimetableRepository(_db.Connections).ForSection(1);

        Assert.Equal(3, entries.Count);
        Assert.All(entries, e => Assert.Equal(e.StartTime.AddHours(1), e.EndTime));
    }
}
