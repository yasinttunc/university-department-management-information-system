namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A staff member with their teaching and advising load.</summary>
public sealed record StaffWorkload(StaffMember Staff, int Sections, int WeeklyTeachingHours, int Advisees);
