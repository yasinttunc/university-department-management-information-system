using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>The numbers shown on the dashboard cards, computed in one place.</summary>
public sealed record DashboardSummary(
    int TotalStudents,
    IReadOnlyDictionary<int, int> StudentsByYear,
    int Sections,
    double AverageOccupancyPercent,
    int FullSections,
    int Advisors,
    double AdviseesPerAdvisor,
    int EvaluationsOutstanding,
    IReadOnlyList<Reminder> NextReminders,
    IReadOnlyList<CalendarEvent> NextEvents);

public sealed class DashboardService(
    IStudentRepository students,
    ICourseRepository courses,
    IStaffRepository staff,
    IAdvisingRepository advising,
    IReminderRepository reminders,
    ICalendarRepository calendar)
{
    public const int UpcomingCount = 3;

    public DashboardSummary Build(DateOnly today)
    {
        var allStudents = students.List();
        var byYear = Enumerable.Range(1, 4).ToDictionary(year => year, year => allStudents.Count(s => s.YearOfStudy == year));

        // Sections without places (capacity 0) are left out of the average.
        var sections = courses.ListSectionsWithOccupancy();
        var withPlaces = sections.Where(s => s.Capacity > 0).ToList();
        var averageOccupancy = withPlaces.Count == 0 ? 0 : Math.Round(withPlaces.Average(s => s.OccupancyPercent), 1);

        var advisors = staff.ListWorkloads().Where(w => w.Advisees > 0).ToList();
        var adviseesPerAdvisor = advisors.Count == 0 ? 0 : Math.Round(advisors.Average(w => (double)w.Advisees), 1);
        var outstanding = advisors.Sum(a => advising.ForAdvisor(a.Staff.Id).Count(s => s.Evaluation is null));

        return new DashboardSummary(
            allStudents.Count,
            byYear,
            sections.Count,
            averageOccupancy,
            withPlaces.Count(s => s.Enrolled >= s.Capacity),
            advisors.Count,
            adviseesPerAdvisor,
            outstanding,
            reminders.ListUpcoming(today).Take(UpcomingCount).ToList(),
            calendar.Upcoming(today, UpcomingCount));
    }
}
