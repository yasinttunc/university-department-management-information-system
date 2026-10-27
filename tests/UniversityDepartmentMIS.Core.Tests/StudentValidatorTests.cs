using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class StudentValidatorTests
{
    private static readonly Student Valid = new(
        0, "26000001", "Alex", "Morgan", "123456789", "alex.morgan@example.com", "+44 (0)7700 900-999",
        null, "Technology and Knowledge Management", 2, 2025);

    [Fact]
    public void Validate_AcceptsAValidStudent()
    {
        Assert.Empty(StudentValidator.Validate(Valid, 2026));
    }

    [Theory]
    [InlineData("StudentNumber", "2600001")]
    [InlineData("StudentNumber", "2600000A")]
    [InlineData("NationalId", "12345678")]
    [InlineData("NationalId", "1234567890")]
    [InlineData("NationalId", "12345678X")]
    [InlineData("Email", "alex.example.com")]
    [InlineData("Email", "alex@example")]
    [InlineData("Phone", "call me")]
    [InlineData("FirstName", "  ")]
    [InlineData("Programme", "")]
    public void Validate_RejectsABadValue(string field, string value)
    {
        var student = field switch
        {
            "StudentNumber" => Valid with { StudentNumber = value },
            "NationalId" => Valid with { NationalId = value },
            "Email" => Valid with { Email = value },
            "Phone" => Valid with { Phone = value },
            "FirstName" => Valid with { FirstName = value },
            _ => Valid with { Programme = value },
        };

        Assert.Equal(field, Assert.Single(StudentValidator.Validate(student, 2026)).Field);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Validate_RejectsYearOfStudyOutsideOneToFour(int year)
    {
        Assert.Equal(nameof(Student.YearOfStudy), Assert.Single(StudentValidator.Validate(Valid with { YearOfStudy = year }, 2026)).Field);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2028)]
    public void Validate_RejectsImplausibleEnrolmentYears(int year)
    {
        Assert.Equal(nameof(Student.EnrolmentYear), Assert.Single(StudentValidator.Validate(Valid with { EnrolmentYear = year }, 2026)).Field);
    }

    [Fact]
    public void Validate_NameLongerThanTheLimit_IsRejected()
    {
        var student = Valid with { LastName = new string('a', StudentValidator.MaxNameLength + 1) };

        Assert.Equal(nameof(Student.LastName), Assert.Single(StudentValidator.Validate(student, 2026)).Field);
    }
}
