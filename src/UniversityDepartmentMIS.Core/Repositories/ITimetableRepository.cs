using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface ITimetableRepository
{
    /// <summary>All slots taught by one lecturer, ordered by day and start time.</summary>
    IReadOnlyList<TimetableEntry> ForLecturer(long staffId);

    /// <summary>All slots of one section, ordered by day and start time.</summary>
    IReadOnlyList<TimetableEntry> ForSection(long sectionId);
}
