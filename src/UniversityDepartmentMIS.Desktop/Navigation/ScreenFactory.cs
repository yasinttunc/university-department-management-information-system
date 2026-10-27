using System.Windows;
using UniversityDepartmentMIS.Core.Navigation;
using UniversityDepartmentMIS.Desktop.Views.Screens;

namespace UniversityDepartmentMIS.Desktop.Navigation;

/// <summary>Creates the view for a menu entry.</summary>
public static class ScreenFactory
{
    public static FrameworkElement Create(NavigationMenuItem item, ScreenContext context, long? recordId) => item.Screen switch
    {
        AppScreen.Dashboard => new DashboardView(context),
        AppScreen.Students => new StudentsView(context),
        AppScreen.Staff => new StaffView(context),
        AppScreen.Courses => new CoursesView(context),
        AppScreen.Timetable => new TimetableView(context, recordId),
        AppScreen.Advising => new AdvisingView(context, recordId),
        AppScreen.Calendar => new CalendarView(context),
        AppScreen.Reminders => new RemindersView(context),
        AppScreen.Mail => new MailView(context),
        AppScreen.DataImport => new DataImportView(context),
        AppScreen.Users => new UsersView(context),
        _ => throw new ArgumentOutOfRangeException(nameof(item), item.Screen, "No view for this screen."),
    };
}
