using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Dialogs;

/// <summary>Shows one student. Admins can add, edit and delete; staff see the record read-only.</summary>
public partial class StudentDialog : Window
{
    private const string DefaultProgramme = "Technology and Knowledge Management";

    private readonly ScreenContext _context;
    private readonly Student? _existing;

    public StudentDialog(ScreenContext context, long? studentId)
    {
        InitializeComponent();
        _context = context;
        var services = context.Services;

        YearInput.ItemsSource = new[] { 1, 2, 3, 4 };
        AdvisorInput.ItemsSource = services.Staff.List();

        _existing = studentId is { } id ? services.Students.Get(id) : null;
        if (_existing is { } student)
        {
            HeaderText.Text = student.FullName;
            Title = $"Student - {student.FullName}";
            NumberInput.Text = student.StudentNumber;
            NationalIdInput.Text = student.NationalId;
            FirstNameInput.Text = student.FirstName;
            LastNameInput.Text = student.LastName;
            EmailInput.Text = student.Email;
            PhoneInput.Text = student.Phone;
            AddressInput.Text = student.Address;
            ProgrammeInput.Text = student.Programme;
            YearInput.SelectedItem = student.YearOfStudy;
            EnrolmentYearInput.Text = student.EnrolmentYear.ToString(CultureInfo.InvariantCulture);

            var advisorId = services.Advising.ForStudent(student.Id)?.AdvisorId;
            AdvisorInput.SelectedItem = ((IEnumerable<StaffMember>)AdvisorInput.ItemsSource).FirstOrDefault(s => s.Id == advisorId);
        }
        else
        {
            HeaderText.Text = "New student";
            Title = "New student";
            ProgrammeInput.Text = DefaultProgramme;
            YearInput.SelectedItem = 1;
            EnrolmentYearInput.Text = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);
        }

        var canEdit = Permissions.CanEditStudents(context.User.Role);
        DeleteButton.Visibility = canEdit && _existing is not null ? Visibility.Visible : Visibility.Collapsed;
        SaveButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        CancelButton.Content = canEdit ? "CANCEL" : "CLOSE";
        if (!canEdit)
        {
            foreach (var input in new Control[]
                     {
                         NumberInput, NationalIdInput, FirstNameInput, LastNameInput, EmailInput, PhoneInput,
                         AddressInput, ProgrammeInput, YearInput, EnrolmentYearInput, AdvisorInput,
                     })
            {
                input.IsEnabled = false;
            }
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(EnrolmentYearInput.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var enrolmentYear))
        {
            ShowErrors(["Enrolment year must be a number, e.g. 2026."]);
            return;
        }

        var student = new Student(
            _existing?.Id ?? 0,
            NumberInput.Text,
            FirstNameInput.Text,
            LastNameInput.Text,
            NationalIdInput.Text,
            EmailInput.Text,
            PhoneInput.Text,
            AddressInput.Text,
            ProgrammeInput.Text,
            YearInput.SelectedItem is int year ? year : 0,
            enrolmentYear);
        var advisorId = (AdvisorInput.SelectedItem as StaffMember)?.Id;

        var service = _context.Services.StudentService;
        var result = _existing is null ? service.Create(student, advisorId) : service.Update(student, advisorId);
        if (!result.Succeeded)
        {
            ShowErrors(result.Errors.Select(error => error.Message).ToList());
            return;
        }

        DialogResult = true;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_existing is null)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"Delete {_existing.FullName} ({_existing.StudentNumber})? Their enrolments and advisor assignment are removed too.",
            "Delete student",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (answer == MessageBoxResult.Yes)
        {
            _context.Services.StudentService.Delete(_existing.Id);
            DialogResult = true;
        }
    }

    private void ShowErrors(IReadOnlyList<string> messages) => ErrorList.ItemsSource = messages;
}
