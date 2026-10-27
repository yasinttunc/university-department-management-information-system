using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class DashboardServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    private DashboardSummary Build(DateOnly today)
    {
        var c = _db.Connections;
        return new DashboardService(
            new StudentRepository(c), new CourseRepository(c), new StaffRepository(c), new AdvisingRepository(c),
            new ReminderRepository(c), new CalendarRepository(c)).Build(today);
    }

    [Fact]
    public void Build_CountsStudentsByYear()
    {
        var summary = Build(new DateOnly(2026, 10, 1));

        Assert.Equal(65, summary.TotalStudents);
        Assert.Equal(new[] { 16, 16, 16, 17 }, summary.StudentsByYear.OrderBy(p => p.Key).Select(p => p.Value));
    }

    [Fact]
    public void Build_AveragesOccupancyOverSectionsWithPlaces()
    {
        var summary = Build(new DateOnly(2026, 10, 1));
        var sections = new CourseRepository(_db.Connections).ListSectionsWithOccupancy().Where(s => s.Capacity > 0).ToList();

        Assert.Equal(34, summary.Sections);
        Assert.Equal(Math.Round(sections.Average(s => s.OccupancyPercent), 1), summary.AverageOccupancyPercent);
        Assert.Equal(sections.Count(s => s.Enrolled >= s.Capacity), summary.FullSections);
    }

    [Fact]
    public void Build_SummarisesAdvising()
    {
        var summary = Build(new DateOnly(2026, 10, 1));

        Assert.Equal(13, summary.Advisors);
        Assert.Equal(Math.Round(65 / 13.0, 1), summary.AdviseesPerAdvisor);
        Assert.Equal(62, summary.EvaluationsOutstanding);
    }

    [Fact]
    public void Build_ListsTheNextThreeRemindersAndEvents()
    {
        var summary = Build(new DateOnly(2026, 11, 10));

        Assert.Equal(2, summary.NextReminders.Count);
        Assert.Equal(new DateOnly(2026, 11, 20), summary.NextReminders[0].RemindOn);
        Assert.Equal(3, summary.NextEvents.Count);
        Assert.Equal(new DateOnly(2026, 11, 16), summary.NextEvents[0].Date);
    }
}
