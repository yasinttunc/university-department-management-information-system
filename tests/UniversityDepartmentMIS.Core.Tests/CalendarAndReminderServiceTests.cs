using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class CalendarAndReminderServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Layout_StartsOnMondayAndCoversWholeWeeks()
    {
        // 1 November 2026 is a Sunday, so the grid starts on Monday 26 October.
        var month = CalendarService.Layout(2026, 11, []);

        Assert.Equal(new DateOnly(2026, 10, 26), month.Days[0].Date);
        Assert.Equal(DayOfWeek.Monday, month.Days[0].Date.DayOfWeek);
        Assert.Equal(DayOfWeek.Sunday, month.Days[month.Days.Count - 1].Date.DayOfWeek);
        Assert.Equal(6, month.Weeks);
        Assert.Equal(30, month.Days.Count(d => d.IsInMonth));
    }

    [Fact]
    public void Layout_MonthStartingOnMonday_HasNoLeadingDays()
    {
        // February 2027 starts on a Monday and has exactly four weeks.
        var month = CalendarService.Layout(2027, 2, []);

        Assert.Equal(new DateOnly(2027, 2, 1), month.Days[0].Date);
        Assert.Equal(4, month.Weeks);
    }

    [Fact]
    public void BuildMonth_AttachesEventsToTheirDays()
    {
        var month = new CalendarService(new CalendarRepository(_db.Connections)).BuildMonth(2026, 11);

        var withEvents = month.Days.Where(d => d.Events.Count > 0).Select(d => d.Date).ToList();
        Assert.Equal(new[] { new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 16) }, withEvents);
    }

    [Fact]
    public void Save_NewEvent_TrimsAndStoresIt()
    {
        var calendar = new CalendarRepository(_db.Connections);
        var service = new CalendarService(calendar);

        var result = service.Save(new CalendarEvent(0, new DateOnly(2026, 11, 25), "  Open day  ", "   "));

        Assert.True(result.Succeeded);
        var saved = Assert.Single(calendar.EventsInMonth(2026, 11), e => e.Id == result.Id);
        Assert.Equal("Open day", saved.Title);
        Assert.Null(saved.Description);
    }

    [Fact]
    public void Save_WithoutTitle_IsRefused()
    {
        var service = new CalendarService(new CalendarRepository(_db.Connections));

        var result = service.Save(new CalendarEvent(0, new DateOnly(2026, 11, 25), " ", null));

        Assert.Equal(nameof(CalendarEvent.Title), Assert.Single(result.Errors).Field);
    }

    [Fact]
    public void Save_ExistingEvent_UpdatesIt_AndMissingEventIsReported()
    {
        var calendar = new CalendarRepository(_db.Connections);
        var service = new CalendarService(calendar);
        var existing = calendar.EventsInMonth(2026, 11)[0];

        Assert.True(service.Save(existing with { Title = "Advisor meetings" }).Succeeded);
        Assert.Equal("Advisor meetings", calendar.EventsInMonth(2026, 11)[0].Title);
        Assert.False(service.Save(existing with { Id = 999 }).Succeeded);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Reminder_WithoutNote_IsRefused(string note)
    {
        var service = new ReminderService(new ReminderRepository(_db.Connections));

        Assert.False(service.Add(new DateOnly(2026, 11, 1), note).Succeeded);
        Assert.Equal(3, service.Upcoming(new DateOnly(2026, 1, 1)).Count);
    }

    [Fact]
    public void Reminder_TooLong_IsRefused_AndValidOneIsSaved()
    {
        var service = new ReminderService(new ReminderRepository(_db.Connections));

        Assert.False(service.Add(new DateOnly(2026, 11, 1), new string('x', ReminderService.MaxNoteLength + 1)).Succeeded);
        Assert.True(service.Add(new DateOnly(2026, 11, 1), " Book rooms ").Succeeded);
        Assert.Equal("Book rooms", service.Upcoming(new DateOnly(2026, 1, 1))[0].Note);
    }
}
