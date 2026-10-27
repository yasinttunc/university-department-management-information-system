using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Core.Tests;

public class BrowseAndWorkloadTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData(null, null, 65)]
    [InlineData(4, null, 17)]
    [InlineData(null, "kaya", 5)]
    [InlineData(2, "zzz", 0)]
    public void Browse_CombinesYearFilterAndSearch(int? year, string? search, int expected)
    {
        var rows = new StudentRepository(_db.Connections).Browse(year, search);

        Assert.Equal(expected, rows.Count);
        if (year is not null)
        {
            Assert.All(rows, r => Assert.True(r.YearOfStudy == year));
        }
    }

    [Fact]
    public void Browse_IncludesEachStudentsAdvisor()
    {
        var rows = new StudentRepository(_db.Connections).Browse(null, null);

        Assert.All(rows, r => Assert.NotNull(r.AdvisorName));
        Assert.Equal(3, rows.Count(r => r.AdvisorName == "Assoc. Prof. Derya Karaca"));
    }

    [Fact]
    public void FindDuplicateFields_IgnoresTheStudentsOwnRecord()
    {
        var students = new StudentRepository(_db.Connections);
        var student = students.Get(1)!;

        Assert.Empty(students.FindDuplicateFields(student));
        Assert.Equal(
            new[] { nameof(Student.StudentNumber), nameof(Student.NationalId), nameof(Student.Email) },
            students.FindDuplicateFields(student with { Id = 2 }));
    }

    [Fact]
    public void ListWorkloads_AddsUpToTheDemoData()
    {
        var workloads = new StaffRepository(_db.Connections).ListWorkloads();

        Assert.Equal(13, workloads.Count);
        Assert.Equal(33, workloads.Sum(w => w.Sections));
        Assert.Equal(78, workloads.Sum(w => w.WeeklyTeachingHours));
        Assert.Equal(65, workloads.Sum(w => w.Advisees));
        Assert.Equal(12, workloads.Single(w => w.Staff.Id == 7).WeeklyTeachingHours);
    }

    [Fact]
    public void Permissions_OnlyAdminsEditStudentsAndAssignSections()
    {
        Assert.True(Permissions.CanEditStudents(UserRole.Admin));
        Assert.False(Permissions.CanEditStudents(UserRole.Staff));
        Assert.True(Permissions.CanAssignSections(UserRole.Admin));
        Assert.False(Permissions.CanAssignSections(UserRole.Staff));
        Assert.True(Permissions.CanRecordEvaluations(UserRole.Staff));
    }
}
