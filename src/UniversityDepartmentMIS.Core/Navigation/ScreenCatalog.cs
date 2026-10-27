using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Navigation;

/// <summary>A menu entry and whether only administrators may open it.</summary>
public sealed record ScreenInfo(AppScreen Screen, string Title, bool AdminOnly);

/// <summary>Decides which screens each role sees in the main menu.</summary>
public static class ScreenCatalog
{
    public static IReadOnlyList<ScreenInfo> All { get; } =
    [
        new(AppScreen.Dashboard, "Dashboard", false),
        new(AppScreen.Students, "Students", false),
        new(AppScreen.Staff, "Staff", false),
        new(AppScreen.Courses, "Courses & sections", false),
        new(AppScreen.Timetable, "Timetable", false),
        new(AppScreen.Advising, "Advising", false),
        new(AppScreen.Calendar, "Calendar", false),
        new(AppScreen.Reminders, "Reminders", false),
        new(AppScreen.Mail, "Mail", false),
        new(AppScreen.DataImport, "Data import", true),
        new(AppScreen.Users, "Users", true),
    ];

    public static IReadOnlyList<ScreenInfo> VisibleTo(UserRole role) =>
        All.Where(screen => role == UserRole.Admin || !screen.AdminOnly).ToList();
}
