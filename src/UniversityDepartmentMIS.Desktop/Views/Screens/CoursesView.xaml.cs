using System.Windows;
using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

public partial class CoursesView : UserControl
{
    private readonly ScreenContext _context;
    private readonly bool _canAssign;
    private readonly bool _ready;

    public CoursesView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;
        _canAssign = Permissions.CanAssignSections(context.User.Role);
        AssignButton.Visibility = _canAssign ? Visibility.Visible : Visibility.Collapsed;

        LevelFilter.ItemsSource = new[] { "All years", "Year 1", "Year 2", "Year 3", "Year 4" };
        LevelFilter.SelectedIndex = 0;
        _ready = true;
        LoadCourses();
    }

    private Course? SelectedCourse => CoursesGrid.SelectedItem as Course;

    private SectionWithOccupancy? SelectedSection => SectionsGrid.SelectedItem as SectionWithOccupancy;

    private void LoadCourses()
    {
        if (!_ready)
        {
            return;
        }

        int? level = LevelFilter.SelectedIndex > 0 ? LevelFilter.SelectedIndex : null;
        CoursesGrid.ItemsSource = _context.Services.Courses.ListCourses(level);
        CoursesGrid.SelectedIndex = 0;
        LoadSections();
    }

    private void LoadSections(long? keepSectionId = null)
    {
        if (SelectedCourse is not { } course)
        {
            SectionsGrid.ItemsSource = null;
            SectionsHeader.Text = "Sections";
            LoadClassList();
            return;
        }

        var sections = _context.Services.Courses.ListSectionsWithOccupancy(course.Id);
        SectionsHeader.Text = $"Sections of {course.Code} {course.Title}";
        SectionsGrid.ItemsSource = sections;
        SectionsGrid.SelectedItem = sections.FirstOrDefault(s => s.SectionId == keepSectionId) ?? sections.FirstOrDefault();
        LoadClassList();
    }

    private void LoadClassList()
    {
        if (SelectedSection is not { } section)
        {
            ClassListGrid.ItemsSource = null;
            ClassListHeader.Text = "Class list";
            AssignButton.IsEnabled = false;
            return;
        }

        var students = _context.Services.Enrolments.StudentsInSection(section.SectionId);
        ClassListGrid.ItemsSource = students;
        ClassListHeader.Text = $"Class list of {section.Label} ({students.Count} of {section.Capacity})";
        AssignButton.IsEnabled = _canAssign;
    }

    private void LevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadCourses();

    private void CoursesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ready && ReferenceEquals(e.OriginalSource, CoursesGrid))
        {
            LoadSections();
        }
    }

    private void SectionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_ready && ReferenceEquals(e.OriginalSource, SectionsGrid))
        {
            LoadClassList();
        }
    }

    private void AssignButton_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedSection is not { } section)
        {
            return;
        }

        var answer = MessageBox.Show(
            Window.GetWindow(this)!,
            $"Replace the class list of {section.Label} with up to {section.Capacity} randomly chosen students? " +
            "Students in this course's year are chosen first, and nobody is placed in two sections of the same course.",
            "Assign students",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question,
            MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes)
        {
            return;
        }

        var result = _context.Services.SectionAssignment.Assign(section.SectionId);
        LoadSections(section.SectionId);
        MessageBox.Show(
            Window.GetWindow(this)!,
            $"{result.Assigned} of {result.Capacity} places filled ({result.AssignedFromCourseYear} from the course's year) " +
            $"from {result.CandidatePool} eligible students.",
            "Assign students",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
