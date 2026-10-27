namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A one-hour teaching slot for a section, Monday to Friday.</summary>
public sealed record TimetableSlot(long Id, long SectionId, DayOfWeek Day, TimeOnly StartTime, TimeOnly EndTime);
