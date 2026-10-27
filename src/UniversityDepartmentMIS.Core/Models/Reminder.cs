namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A dated to-do note for the department office.</summary>
public sealed record Reminder(long Id, DateOnly RemindOn, string Note);
