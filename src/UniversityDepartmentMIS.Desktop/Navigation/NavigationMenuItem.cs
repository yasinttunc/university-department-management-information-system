using MaterialDesignThemes.Wpf;
using UniversityDepartmentMIS.Core.Navigation;

namespace UniversityDepartmentMIS.Desktop.Navigation;

/// <summary>One entry in the left-hand menu.</summary>
public sealed record NavigationMenuItem(AppScreen Screen, string Title, PackIconKind Icon)
{
    private static readonly IReadOnlyDictionary<AppScreen, PackIconKind> Icons = new Dictionary<AppScreen, PackIconKind>
    {
        [AppScreen.Dashboard] = PackIconKind.ViewDashboard,
        [AppScreen.Students] = PackIconKind.AccountSchool,
        [AppScreen.Staff] = PackIconKind.AccountTie,
        [AppScreen.Courses] = PackIconKind.BookOpenPageVariant,
        [AppScreen.Timetable] = PackIconKind.CalendarClock,
        [AppScreen.Advising] = PackIconKind.AccountSupervisor,
        [AppScreen.Calendar] = PackIconKind.CalendarMonth,
        [AppScreen.Reminders] = PackIconKind.BellOutline,
        [AppScreen.Mail] = PackIconKind.EmailOutline,
        [AppScreen.DataImport] = PackIconKind.DatabaseImport,
        [AppScreen.Users] = PackIconKind.AccountKey,
    };

    public static NavigationMenuItem From(ScreenInfo screen) => new(screen.Screen, screen.Title, Icons[screen.Screen]);

    // Used by UI Automation and screen readers as the item's name.
    public override string ToString() => Title;
}
