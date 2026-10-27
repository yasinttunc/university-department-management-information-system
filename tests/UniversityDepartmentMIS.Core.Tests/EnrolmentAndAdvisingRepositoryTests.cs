using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Tests;

public class EnrolmentAndAdvisingRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void StudentsInSection_MatchesTheEnrolmentTable()
    {
        var students = new EnrolmentRepository(_db.Connections).StudentsInSection(1);

        Assert.Equal(_db.Scalar("SELECT count(*) FROM enrolments WHERE section_id = 1"), students.Count);
        Assert.Contains(students, s => s.Id == 17);
    }

    [Fact]
    public void ReplaceSectionEnrolments_ReplacesTheWholeClassList()
    {
        var enrolments = new EnrolmentRepository(_db.Connections);

        enrolments.ReplaceSectionEnrolments(1, [1, 2, 3, 3]);

        Assert.Equal(new long[] { 1, 2, 3 }, enrolments.StudentsInSection(1).Select(s => s.Id));
    }

    [Fact]
    public void ReplaceSectionEnrolments_WithUnknownStudent_ChangesNothing()
    {
        var enrolments = new EnrolmentRepository(_db.Connections);
        var before = enrolments.StudentsInSection(1).Select(s => s.Id).ToList();

        Assert.Throws<SqliteException>(() => enrolments.ReplaceSectionEnrolments(1, [1, 9999]));

        Assert.Equal(before, enrolments.StudentsInSection(1).Select(s => s.Id));
    }

    [Fact]
    public void ForAdvisor_ReturnsAdviseesWithEvaluations()
    {
        var advisees = new AdvisingRepository(_db.Connections).ForAdvisor(1);

        Assert.Equal(3, advisees.Count);
        Assert.Equal(2, advisees.Count(a => a.Evaluation == Evaluation.Successful));
        Assert.Single(advisees, a => a.Evaluation == Evaluation.Average);
    }

    [Fact]
    public void SetEvaluation_RecordsAndClears()
    {
        var advising = new AdvisingRepository(_db.Connections);

        Assert.True(advising.SetEvaluation(1, Evaluation.Unsuccessful));
        Assert.Equal(Evaluation.Unsuccessful, advising.ForStudent(1)!.Evaluation);

        Assert.True(advising.SetEvaluation(1, null));
        Assert.Null(advising.ForStudent(1)!.Evaluation);
    }

    [Fact]
    public void Assign_ToANewAdvisor_MovesTheStudentAndClearsTheEvaluation()
    {
        var advising = new AdvisingRepository(_db.Connections);
        var student = advising.ForAdvisor(1)[0];
        Assert.NotNull(student.Evaluation);

        advising.Assign(student.StudentId, 2);

        Assert.Equal(new AdvisorAssignment(student.StudentId, 2, null), advising.ForStudent(student.StudentId));
        Assert.Equal(2, advising.ForAdvisor(1).Count);
    }

    [Fact]
    public void Assign_ToTheSameAdvisor_KeepsTheEvaluation()
    {
        var advising = new AdvisingRepository(_db.Connections);
        var student = advising.ForAdvisor(1)[0];

        advising.Assign(student.StudentId, 1);

        Assert.Equal(student.Evaluation, advising.ForStudent(student.StudentId)!.Evaluation);
    }
}
