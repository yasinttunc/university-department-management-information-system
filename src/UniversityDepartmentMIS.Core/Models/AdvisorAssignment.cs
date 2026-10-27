namespace UniversityDepartmentMIS.Core.Models;

/// <summary>Links a student to their advisor. The evaluation stays empty until the advisor records one.</summary>
public sealed record AdvisorAssignment(long StudentId, long AdvisorId, Evaluation? Evaluation);
