using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;

namespace UniversityDepartmentMIS.Core.Tests;

public class DatabaseInitializerTests
{
    [Fact]
    public void EnsureCreated_OnNewFile_CreatesDatabaseAndReturnsTrue()
    {
        using var db = new TestDatabase(initialise: false);

        Assert.True(DatabaseInitializer.EnsureCreated(db.Connections));
        Assert.True(File.Exists(db.Path));
        Assert.Equal(10, db.Scalar("SELECT count(*) FROM sqlite_master WHERE type = 'table'"));
        Assert.Equal(DatabaseInitializer.SchemaVersion, db.Scalar("PRAGMA user_version"));
    }

    [Theory]
    [InlineData("users", 2)]
    [InlineData("staff", 13)]
    [InlineData("students", 65)]
    [InlineData("courses", 21)]
    [InlineData("sections", 34)]
    [InlineData("timetable_slots", 81)]
    [InlineData("enrolments", 542)]
    [InlineData("advisor_assignments", 65)]
    [InlineData("calendar_events", 6)]
    [InlineData("reminders", 3)]
    public void Seed_LoadsExpectedRowCounts(string table, int expected)
    {
        using var db = new TestDatabase();

        // The table name comes from the fixed InlineData above, never from input.
        Assert.Equal((long)expected, db.Scalar($"SELECT count(*) FROM {table}"));
    }

    [Fact]
    public void EnsureCreated_OnExistingDatabase_ChangesNothing()
    {
        using var db = new TestDatabase();
        db.Execute("DELETE FROM reminders");

        Assert.False(DatabaseInitializer.EnsureCreated(db.Connections));
        Assert.Equal(0, db.Scalar("SELECT count(*) FROM reminders"));
        Assert.Equal(65, db.Scalar("SELECT count(*) FROM students"));
    }

    [Fact]
    public void EnsureCreated_OnForeignDatabase_Throws()
    {
        using var db = new TestDatabase(initialise: false);
        db.Execute("CREATE TABLE something_else (id INTEGER)");

        var error = Assert.Throws<InvalidOperationException>(() => DatabaseInitializer.EnsureCreated(db.Connections));
        Assert.Contains("schema version 0", error.Message);
    }

    [Fact]
    public void Seed_HasNoForeignKeyViolations()
    {
        using var db = new TestDatabase();
        using var connection = db.Connections.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check";
        using var reader = command.ExecuteReader();

        Assert.False(reader.Read());
    }

    [Fact]
    public void Seed_NoSectionIsOverCapacity()
    {
        using var db = new TestDatabase();

        Assert.Equal(0, db.Scalar(
            "SELECT count(*) FROM sections s WHERE (SELECT count(*) FROM enrolments e WHERE e.section_id = s.id) > s.capacity"));
    }

    [Fact]
    public void Connections_EnforceForeignKeys()
    {
        using var db = new TestDatabase();

        Assert.Equal(1, db.Scalar("PRAGMA foreign_keys"));
        Assert.Throws<SqliteException>(() => db.Execute("INSERT INTO enrolments (section_id, student_id) VALUES (1, 9999)"));
    }

    [Fact]
    public void Schema_RejectsNationalIdThatIsNotNineCharacters()
    {
        using var db = new TestDatabase();

        Assert.Throws<SqliteException>(() => db.Execute("UPDATE students SET national_id = '12345' WHERE id = 1"));
    }

    [Fact]
    public void DatabasePaths_UsesEnvironmentOverride()
    {
        var path = Path.Combine(Path.GetTempPath(), "udmis-override.db");
        var previous = Environment.GetEnvironmentVariable(DatabasePaths.OverrideVariable);
        try
        {
            Environment.SetEnvironmentVariable(DatabasePaths.OverrideVariable, path);
            Assert.Equal(Path.GetFullPath(path), DatabasePaths.Resolve());
        }
        finally
        {
            Environment.SetEnvironmentVariable(DatabasePaths.OverrideVariable, previous);
        }
    }
}
