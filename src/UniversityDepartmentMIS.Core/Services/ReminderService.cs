using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Adds and removes reminders after checking the note.</summary>
public sealed class ReminderService(IReminderRepository reminders)
{
    public const int MaxNoteLength = 200;

    public IReadOnlyList<Reminder> Upcoming(DateOnly today) => reminders.ListUpcoming(today);

    public OperationResult Add(DateOnly remindOn, string? note)
    {
        var text = note?.Trim() ?? string.Empty;
        if (text.Length == 0 || text.Length > MaxNoteLength)
        {
            return OperationResult.Failure(nameof(Reminder.Note), $"Write a note of up to {MaxNoteLength} characters.");
        }

        return OperationResult.Success(reminders.Add(new Reminder(0, remindOn, text)));
    }

    public bool Delete(long id) => reminders.Delete(id);
}
