using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class StudentServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly StudentRepository _students;
    private readonly AdvisingRepository _advising;
    private readonly StudentService _service;

    public StudentServiceTests()
    {
        _students = new StudentRepository(_db.Connections);
        _advising = new AdvisingRepository(_db.Connections);
        _service = new StudentService(_students, new StaffRepository(_db.Connections), _advising, new TestClock(2026));
    }

    public void Dispose() => _db.Dispose();

    private static Student Valid() => new(
        0, "26000001", " Alex ", "Morgan", "123456789", "alex.morgan@example.com", " ", null,
        "Technology and Knowledge Management", 1, 2026);

    [Fact]
    public void Create_ValidStudent_SavesTrimmedValuesAndAdvisor()
    {
        var result = _service.Create(Valid(), advisorId: 3);

        Assert.True(result.Succeeded);
        var saved = _students.Get(result.Id!.Value)!;
        Assert.Equal("Alex", saved.FirstName);
        Assert.Null(saved.Phone);
        Assert.Equal(3, _advising.ForStudent(saved.Id)!.AdvisorId);
    }

    [Fact]
    public void Create_InvalidStudent_ReturnsEveryProblemAndSavesNothing()
    {
        var result = _service.Create(Valid() with { NationalId = "12", Email = "not-an-email", YearOfStudy = 7 }, null);

        Assert.False(result.Succeeded);
        Assert.Equal(
            new[] { nameof(Student.NationalId), nameof(Student.Email), nameof(Student.YearOfStudy) },
            result.Errors.Select(e => e.Field));
        Assert.Equal(65, _students.List().Count);
    }

    [Theory]
    [InlineData(nameof(Student.StudentNumber))]
    [InlineData(nameof(Student.NationalId))]
    [InlineData(nameof(Student.Email))]
    public void Create_WithValueAnotherStudentUses_IsRefused(string field)
    {
        var existing = _students.Get(1)!;
        var student = field switch
        {
            nameof(Student.StudentNumber) => Valid() with { StudentNumber = existing.StudentNumber },
            nameof(Student.NationalId) => Valid() with { NationalId = existing.NationalId },
            _ => Valid() with { Email = existing.Email.ToUpperInvariant() },
        };

        var result = _service.Create(student, null);

        Assert.Equal(field, Assert.Single(result.Errors).Field);
    }

    [Fact]
    public void Create_WithUnknownAdvisor_IsRefused()
    {
        var result = _service.Create(Valid(), advisorId: 999);

        Assert.Equal("Advisor", Assert.Single(result.Errors).Field);
        Assert.Equal(65, _students.List().Count);
    }

    [Fact]
    public void Update_KeepingItsOwnUniqueValues_Succeeds()
    {
        var student = _students.Get(1)! with { Address = "5 Other Street, Ankara" };

        var result = _service.Update(student, advisorId: 2);

        Assert.True(result.Succeeded);
        Assert.Equal("5 Other Street, Ankara", _students.Get(1)!.Address);
        Assert.Equal(2, _advising.ForStudent(1)!.AdvisorId);
    }

    [Fact]
    public void Update_MissingStudent_IsRefused()
    {
        Assert.False(_service.Update(Valid() with { Id = 999 }, null).Succeeded);
    }
}
