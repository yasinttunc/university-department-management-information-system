using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface IReminderRepository
{
    /// <summary>Reminders due on or after the given date, soonest first.</summary>
    IReadOnlyList<Reminder> ListUpcoming(DateOnly from);

    long Add(Reminder reminder);

    bool Delete(long id);
}
