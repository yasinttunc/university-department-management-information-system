namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A department event shown on the calendar.</summary>
public sealed record CalendarEvent(long Id, DateOnly Date, string Title, string? Description);
