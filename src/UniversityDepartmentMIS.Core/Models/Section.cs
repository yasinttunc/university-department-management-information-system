namespace UniversityDepartmentMIS.Core.Models;

/// <summary>One taught group of a course, with its own room, capacity and lecturer.</summary>
public sealed record Section(long Id, long CourseId, string SectionNumber, string? Room, int Capacity, long? LecturerId);
