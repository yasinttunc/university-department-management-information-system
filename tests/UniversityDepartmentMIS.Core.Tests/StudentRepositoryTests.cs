using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Tests;

public class StudentRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly StudentRepository _students;

    public StudentRepositoryTests() => _students = new StudentRepository(_db.Connections);

    public void Dispose() => _db.Dispose();

    private static Student NewStudent(string number = "26000001", string nationalId = "123456789") => new(
        0, number, "Alex", "Morgan", nationalId, $"alex.morgan.{number}@example.com", "+44 7700 900999",
        "1 Test Road, Swansea", "Technology and Knowledge Management", 1, 2026);

    [Fact]
    public void List_ReturnsAllStudentsOrderedByNumber()
    {
        var all = _students.List();

        Assert.Equal(65, all.Count);
        Assert.Equal(all.OrderBy(s => s.StudentNumber, StringComparer.Ordinal).Select(s => s.Id), all.Select(s => s.Id));
    }

    [Theory]
    [InlineData(1, 16)]
    [InlineData(4, 17)]
    public void List_FiltersByYearOfStudy(int year, int expected)
    {
        var students = _students.List(year);

        Assert.Equal(expected, students.Count);
        Assert.All(students, s => Assert.Equal(year, s.YearOfStudy));
    }

    [Theory]
    [InlineData("kaya", 5)]
    [InlineData("KAYA", 5)]
    [InlineData("Esra Kaya", 1)]
    [InlineData("22097500", 1)]
    [InlineData("zzz-no-match", 0)]
    public void Search_MatchesNameNumberAndEmail(string text, int expected)
    {
        Assert.Equal(expected, _students.Search(text).Count);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("_")]
    [InlineData("' OR 1=1 --")]
    public void Search_TreatsWildcardsAndQuotesAsPlainText(string text)
    {
        Assert.Empty(_students.Search(text));
    }

    [Fact]
    public void Add_ThenGet_ReturnsTheSameStudent()
    {
        var id = _students.Add(NewStudent());

        Assert.Equal(NewStudent() with { Id = id }, _students.Get(id));
        Assert.Equal(66, _students.List().Count);
    }

    [Fact]
    public void Add_WithDuplicateNationalId_IsRejected()
    {
        var existing = _students.Get(1)!;

        Assert.Throws<SqliteException>(() => _students.Add(NewStudent(nationalId: existing.NationalId)));
    }

    [Fact]
    public void Update_ChangesStoredFields()
    {
        var student = _students.Get(1)! with { YearOfStudy = 2, Phone = null, Address = "2 New Street, Ankara" };

        Assert.True(_students.Update(student));
        Assert.Equal(student, _students.Get(1));
    }

    [Fact]
    public void Delete_RemovesStudentWithEnrolmentsAndAdvisor()
    {
        Assert.True(_db.Scalar("SELECT count(*) FROM enrolments WHERE student_id = 1") > 0);

        Assert.True(_students.Delete(1));

        Assert.Null(_students.Get(1));
        Assert.Equal(0, _db.Scalar("SELECT count(*) FROM enrolments WHERE student_id = 1"));
        Assert.Equal(0, _db.Scalar("SELECT count(*) FROM advisor_assignments WHERE student_id = 1"));
        Assert.False(_students.Delete(1));
    }
}
