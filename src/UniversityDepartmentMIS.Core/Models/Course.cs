namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A course in the department's catalogue. Credits are unknown for a few courses, so they are optional.</summary>
public sealed record Course(long Id, string Code, string Title, int? Credits, int? Ects, int YearLevel);
