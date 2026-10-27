namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A timetable slot joined with what is taught in it and by whom.</summary>
public sealed record TimetableEntry(
    long SectionId,
    DayOfWeek Day,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string CourseCode,
    string CourseTitle,
    string SectionNumber,
    string? Room,
    long? LecturerId,
    string? LecturerName)
{
    public string Label => $"{CourseCode}-{SectionNumber}";
}
