using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Desktop;

/// <summary>
/// Builds the repositories and services once and hands them to the windows. A small app does not need a
/// dependency-injection container; this class is the single place where objects are wired together.
/// </summary>
public sealed class AppServices
{
    private AppServices(ConnectionFactory connections)
    {
        Connections = connections;
        Users = new UserRepository(connections);
        Students = new StudentRepository(connections);
        Staff = new StaffRepository(connections);
        Courses = new CourseRepository(connections);
        Timetable = new TimetableRepository(connections);
        Enrolments = new EnrolmentRepository(connections);
        Advising = new AdvisingRepository(connections);
        Calendar = new CalendarRepository(connections);
        Reminders = new ReminderRepository(connections);
        Auth = new AuthService(Users);
        StudentService = new StudentService(Students, Staff, Advising);
        SectionAssignment = new SectionAssignmentService(Courses, Students, Enrolments);
        TimetableService = new TimetableService(Timetable);
        Dashboard = new DashboardService(Students, Courses, Staff, Advising, Reminders, Calendar);
        CalendarService = new CalendarService(Calendar);
        ReminderService = new ReminderService(Reminders);
        UserService = new UserService(Users);
        ExcelImport = new ExcelImportService(Students);
    }

    public ConnectionFactory Connections { get; }

    public IUserRepository Users { get; }

    public IStudentRepository Students { get; }

    public IStaffRepository Staff { get; }

    public ICourseRepository Courses { get; }

    public ITimetableRepository Timetable { get; }

    public IEnrolmentRepository Enrolments { get; }

    public IAdvisingRepository Advising { get; }

    public ICalendarRepository Calendar { get; }

    public IReminderRepository Reminders { get; }

    public AuthService Auth { get; }

    public StudentService StudentService { get; }

    public SectionAssignmentService SectionAssignment { get; }

    public TimetableService TimetableService { get; }

    public DashboardService Dashboard { get; }

    public CalendarService CalendarService { get; }

    public ReminderService ReminderService { get; }

    public UserService UserService { get; }

    public ExcelImportService ExcelImport { get; }

    /// <summary>Opens (and if needed creates) the database at <paramref name="databasePath"/>.</summary>
    public static AppServices Create(string databasePath)
    {
        var connections = new ConnectionFactory(databasePath);
        DatabaseInitializer.EnsureCreated(connections);
        return new AppServices(connections);
    }
}
