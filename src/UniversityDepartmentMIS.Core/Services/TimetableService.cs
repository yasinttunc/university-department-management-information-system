using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>One hour of one weekday in the timetable grid. More than one entry means a clash.</summary>
public sealed record TimetableCell(DayOfWeek Day, TimeOnly Start, IReadOnlyList<TimetableEntry> Entries)
{
    public bool IsFree => Entries.Count == 0;

    public bool HasClash => Entries.Count > 1;
}

/// <summary>A Monday-to-Friday, 09:00-18:00 grid of one-hour cells.</summary>
public sealed record WeekTimetable(
    IReadOnlyList<DayOfWeek> Days,
    IReadOnlyList<TimeOnly> SlotStarts,
    IReadOnlyList<TimetableCell> Cells,
    IReadOnlyList<TimetableEntry> OutsideGrid)
{
    public TimetableCell this[DayOfWeek day, TimeOnly start] =>
        Cells.FirstOrDefault(c => c.Day == day && c.Start == start)
        ?? throw new ArgumentOutOfRangeException(nameof(start), $"{day} {start} is not in the grid.");

    public int TeachingHours => Cells.Sum(c => c.Entries.Count) + OutsideGrid.Count;

    public IReadOnlyList<TimetableCell> Clashes => Cells.Where(c => c.HasClash).ToList();
}

/// <summary>Builds the weekly grid shown on the timetable screen.</summary>
public sealed class TimetableService(ITimetableRepository timetable)
{
    public const int SlotsPerDay = 9;

    public static readonly TimeOnly FirstSlotStart = new(9, 0);

    public static IReadOnlyList<DayOfWeek> Weekdays { get; } =
        [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday];

    public WeekTimetable ForLecturer(long staffId) => Build(timetable.ForLecturer(staffId));

    public WeekTimetable ForSection(long sectionId) => Build(timetable.ForSection(sectionId));

    /// <summary>Places each entry in the cell of its day and start hour; entries outside the grid are listed separately.</summary>
    public static WeekTimetable Build(IEnumerable<TimetableEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        var starts = Enumerable.Range(0, SlotsPerDay).Select(i => FirstSlotStart.AddHours(i)).ToList();
        var all = entries.ToList();

        var cells = (
            from day in Weekdays
            from start in starts
            select new TimetableCell(day, start, all.Where(e => e.Day == day && e.StartTime == start).ToList())
        ).ToList();

        var placed = cells.SelectMany(c => c.Entries).ToHashSet();
        var outside = all.Where(e => !placed.Contains(e)).ToList();
        return new WeekTimetable(Weekdays, starts, cells, outside);
    }
}
