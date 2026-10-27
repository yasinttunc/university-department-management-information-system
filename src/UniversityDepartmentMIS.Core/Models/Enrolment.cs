namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A student's place in a section.</summary>
public sealed record Enrolment(long SectionId, long StudentId);
