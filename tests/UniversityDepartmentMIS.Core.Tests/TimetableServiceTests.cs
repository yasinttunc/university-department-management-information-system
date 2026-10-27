using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class TimetableServiceTests
{
    private static TimetableEntry Entry(DayOfWeek day, int hour, string code = "TKM101", string section = "11") =>
        new(1, day, new TimeOnly(hour, 0), new TimeOnly(hour + 1, 0), code, "Test course", section, "C-101", 1, "Dr. Test");

    [Fact]
    public void Build_MakesAFiveByNineGrid()
    {
        var week = TimetableService.Build([]);

        Assert.Equal(5, week.Days.Count);
        Assert.Equal(new TimeOnly(9, 0), week.SlotStarts[0]);
        Assert.Equal(new TimeOnly(17, 0), week.SlotStarts[week.SlotStarts.Count - 1]);
        Assert.Equal(45, week.Cells.Count);
        Assert.All(week.Cells, c => Assert.True(c.IsFree));
    }

    [Fact]
    public void Build_PlacesEntriesInTheirDayAndHour()
    {
        var week = TimetableService.Build([Entry(DayOfWeek.Wednesday, 13)]);

        var cell = week[DayOfWeek.Wednesday, new TimeOnly(13, 0)];
        Assert.Equal("TKM101-11", Assert.Single(cell.Entries).Label);
        Assert.Equal(1, week.TeachingHours);
        Assert.Equal(44, week.Cells.Count(c => c.IsFree));
    }

    [Fact]
    public void Build_MarksTwoSectionsInOneHourAsAClash()
    {
        var week = TimetableService.Build([Entry(DayOfWeek.Monday, 9, section: "11"), Entry(DayOfWeek.Monday, 9, section: "12")]);

        var clash = Assert.Single(week.Clashes);
        Assert.Equal((DayOfWeek.Monday, new TimeOnly(9, 0)), (clash.Day, clash.Start));
    }

    [Fact]
    public void Build_ListsEntriesOutsideTheGridSeparately()
    {
        var week = TimetableService.Build([Entry(DayOfWeek.Saturday, 10), Entry(DayOfWeek.Friday, 18)]);

        Assert.Equal(2, week.OutsideGrid.Count);
        Assert.All(week.Cells, c => Assert.True(c.IsFree));
        Assert.Equal(2, week.TeachingHours);
    }

    [Fact]
    public void ForLecturer_MatchesTheDemoData()
    {
        using var db = new TestDatabase();
        var service = new TimetableService(new TimetableRepository(db.Connections));

        var week = service.ForLecturer(7);

        Assert.Equal(12, week.TeachingHours);
        Assert.Empty(week.Clashes);
        Assert.Empty(week.OutsideGrid);
    }
}
