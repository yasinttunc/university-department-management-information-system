using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class AdvisingRepository(ConnectionFactory connections) : IAdvisingRepository
{
    public IReadOnlyList<StudentSummary> ForAdvisor(long staffId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT s.id, s.student_number, s.first_name, s.last_name, s.year_of_study, a.evaluation " +
            "FROM advisor_assignments a JOIN students s ON s.id = a.student_id " +
            "WHERE a.advisor_id = @staff ORDER BY s.student_number",
            ("@staff", staffId));
        return command.ReadAll(r => new StudentSummary(
            r.GetInt64(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetInt32(4), ParseEvaluation(r.GetNullableString(5))));
    }

    public AdvisorAssignment? ForStudent(long studentId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "SELECT student_id, advisor_id, evaluation FROM advisor_assignments WHERE student_id = @student",
            ("@student", studentId));
        return command.ReadSingleOrDefault(r => new AdvisorAssignment(
            r.GetInt64(0), r.GetInt64(1), ParseEvaluation(r.GetNullableString(2))));
    }

    public void Assign(long studentId, long advisorId)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "INSERT INTO advisor_assignments (student_id, advisor_id, evaluation) VALUES (@student, @advisor, NULL) " +
            "ON CONFLICT (student_id) DO UPDATE SET advisor_id = excluded.advisor_id, evaluation = NULL " +
            "WHERE advisor_assignments.advisor_id <> excluded.advisor_id",
            ("@student", studentId), ("@advisor", advisorId));
        command.ExecuteNonQuery();
    }

    public bool SetEvaluation(long studentId, Evaluation? evaluation)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "UPDATE advisor_assignments SET evaluation = @evaluation WHERE student_id = @student",
            ("@evaluation", evaluation?.ToString()), ("@student", studentId));
        return command.ExecuteNonQuery() == 1;
    }

    private static Evaluation? ParseEvaluation(string? value) =>
        value is null ? null : Enum.Parse<Evaluation>(value);
}
