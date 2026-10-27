using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Core.Tests;

public class CalendarReminderAndUserRepositoryTests : IDisposable
{
    private readonly TestDatabase _db = new();

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData(2026, 11, 2)]
    [InlineData(2026, 12, 1)]
    [InlineData(2026, 10, 0)]
    [InlineData(2027, 5, 1)]
    public void EventsInMonth_ReturnsOnlyThatMonth(int year, int month, int expected)
    {
        var events = new CalendarRepository(_db.Connections).EventsInMonth(year, month);

        Assert.Equal(expected, events.Count);
        Assert.All(events, e => Assert.Equal((year, month), (e.Date.Year, e.Date.Month)));
    }

    [Fact]
    public void CalendarEvent_AddUpdateDelete()
    {
        var calendar = new CalendarRepository(_db.Connections);

        var id = calendar.Add(new CalendarEvent(0, new DateOnly(2026, 11, 30), "Open day", null));
        Assert.Equal(3, calendar.EventsInMonth(2026, 11).Count);

        var moved = new CalendarEvent(id, new DateOnly(2026, 12, 1), "Open day", "Main hall");
        Assert.True(calendar.Update(moved));
        Assert.Contains(moved, calendar.EventsInMonth(2026, 12));

        Assert.True(calendar.Delete(id));
        Assert.DoesNotContain(calendar.EventsInMonth(2026, 12), e => e.Id == id);
    }

    [Fact]
    public void Reminders_ListUpcomingAddAndDelete()
    {
        var reminders = new ReminderRepository(_db.Connections);

        Assert.Equal(3, reminders.ListUpcoming(new DateOnly(2026, 1, 1)).Count);
        Assert.Single(reminders.ListUpcoming(new DateOnly(2026, 12, 1)));

        var id = reminders.Add(new Reminder(0, new DateOnly(2026, 11, 1), "Order exam papers"));
        Assert.Equal(id, reminders.ListUpcoming(new DateOnly(2026, 1, 1))[0].Id);

        Assert.True(reminders.Delete(id));
        Assert.False(reminders.Delete(id));
    }

    [Fact]
    public void Users_FindByUsername_IgnoresCase()
    {
        var users = new UserRepository(_db.Connections);

        Assert.Equal(UserRole.Admin, users.FindByUsername("Admin")!.Role);
        Assert.Null(users.FindByUsername("nobody"));
    }

    [Fact]
    public void Users_AddAndUpdatePassword()
    {
        var users = new UserRepository(_db.Connections);
        var auth = new AuthService(users);

        var id = users.Add("office", PasswordHasher.Hash("Office2026"), UserRole.Staff, "Technology and Knowledge Management");
        Assert.True(auth.Login("office", "Office2026").Succeeded);

        Assert.True(users.UpdatePasswordHash(id, PasswordHasher.Hash("Changed2026")));
        Assert.False(auth.Login("office", "Office2026").Succeeded);
        Assert.True(auth.Login("office", "Changed2026").Succeeded);
    }

    [Fact]
    public void Users_DuplicateUsername_IsRejected()
    {
        var users = new UserRepository(_db.Connections);

        Assert.Throws<SqliteException>(() => users.Add("ADMIN", "pbkdf2$x", UserRole.Staff, "Test"));
    }
}
