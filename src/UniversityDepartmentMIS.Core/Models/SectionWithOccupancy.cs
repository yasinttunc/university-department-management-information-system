namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A section joined with its course, lecturer and current number of enrolled students.</summary>
public sealed record SectionWithOccupancy(
    long SectionId,
    long CourseId,
    string CourseCode,
    string CourseTitle,
    string SectionNumber,
    string? Room,
    int Capacity,
    int Enrolled,
    long? LecturerId,
    string? LecturerName)
{
    /// <summary>Course code and section number, e.g. "TKM405-41".</summary>
    public string Label => $"{CourseCode}-{SectionNumber}";

    public int FreePlaces => Math.Max(0, Capacity - Enrolled);

    /// <summary>Enrolled students as a percentage of capacity (0 when the capacity is 0).</summary>
    public double OccupancyPercent => Capacity == 0 ? 0 : Math.Round(100.0 * Enrolled / Capacity, 1);
}
