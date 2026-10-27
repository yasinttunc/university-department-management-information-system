using System.Text.RegularExpressions;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Checks a student record before it is saved. Field names match the <see cref="Student"/> properties.</summary>
public static partial class StudentValidator
{
    public const int StudentNumberLength = 8;
    public const int NationalIdLength = 9;
    public const int MaxNameLength = 50;
    public const int FirstEnrolmentYear = 2000;

    public static IReadOnlyList<ValidationError> Validate(Student student, int currentYear)
    {
        ArgumentNullException.ThrowIfNull(student);
        var errors = new List<ValidationError>();

        void Require(bool condition, string field, string message)
        {
            if (!condition)
            {
                errors.Add(new ValidationError(field, message));
            }
        }

        Require(IsDigits(student.StudentNumber, StudentNumberLength), nameof(Student.StudentNumber),
            $"Student number must be exactly {StudentNumberLength} digits.");
        Require(IsName(student.FirstName), nameof(Student.FirstName),
            $"First name is required (up to {MaxNameLength} characters).");
        Require(IsName(student.LastName), nameof(Student.LastName),
            $"Last name is required (up to {MaxNameLength} characters).");
        Require(IsDigits(student.NationalId, NationalIdLength), nameof(Student.NationalId),
            $"National ID must be exactly {NationalIdLength} digits.");
        Require(student.Email is not null && EmailPattern().IsMatch(student.Email), nameof(Student.Email),
            "Enter a valid email address.");
        Require(string.IsNullOrEmpty(student.Phone) || PhonePattern().IsMatch(student.Phone), nameof(Student.Phone),
            "Phone may contain only digits, spaces, '+', '-' and brackets (up to 20 characters).");
        Require(!string.IsNullOrWhiteSpace(student.Programme), nameof(Student.Programme), "Programme is required.");
        Require(student.YearOfStudy is >= 1 and <= 4, nameof(Student.YearOfStudy), "Year of study must be 1 to 4.");
        Require(student.EnrolmentYear >= FirstEnrolmentYear && student.EnrolmentYear <= currentYear + 1,
            nameof(Student.EnrolmentYear), $"Enrolment year must be between {FirstEnrolmentYear} and {currentYear + 1}.");

        return errors;
    }

    private static bool IsDigits(string? value, int length) =>
        value is not null && value.Length == length && value.All(char.IsAsciiDigit);

    private static bool IsName(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= MaxNameLength;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[0-9+\-() ]{1,20}$")]
    private static partial Regex PhonePattern();
}
