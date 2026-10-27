using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface IAdvisingRepository
{
    /// <summary>The advisees of one staff member, by student number.</summary>
    IReadOnlyList<StudentSummary> ForAdvisor(long staffId);

    AdvisorAssignment? ForStudent(long studentId);

    /// <summary>Gives the student an advisor. Changing to a different advisor clears the evaluation.</summary>
    void Assign(long studentId, long advisorId);

    /// <summary>Records or clears the advisor's evaluation. Returns false if the student has no advisor.</summary>
    bool SetEvaluation(long studentId, Evaluation? evaluation);
}
