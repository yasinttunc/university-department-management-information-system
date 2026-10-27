using System.Globalization;
using Microsoft.Data.Sqlite;

namespace UniversityDepartmentMIS.Core.Database;

/// <summary>
/// Small helpers shared by the repositories. Every query is a constant string and every value is
/// passed as a parameter; nothing here builds SQL from input.
/// </summary>
internal static class SqliteExtensions
{
    public const string DateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm";

    public static SqliteCommand Sql(
        this SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        return command;
    }

    public static List<T> ReadAll<T>(this SqliteCommand command, Func<SqliteDataReader, T> map)
    {
        var results = new List<T>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            results.Add(map(reader));
        }

        return results;
    }

    public static T? ReadSingleOrDefault<T>(this SqliteCommand command, Func<SqliteDataReader, T> map)
        where T : class
    {
        using var reader = command.ExecuteReader();
        return reader.Read() ? map(reader) : null;
    }

    public static string? GetNullableString(this SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    public static int? GetNullableInt32(this SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);

    public static long? GetNullableInt64(this SqliteDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);

    public static DateOnly GetDate(this SqliteDataReader reader, int ordinal) =>
        DateOnly.ParseExact(reader.GetString(ordinal), DateFormat, CultureInfo.InvariantCulture);

    public static TimeOnly GetTime(this SqliteDataReader reader, int ordinal) =>
        TimeOnly.ParseExact(reader.GetString(ordinal), TimeFormat, CultureInfo.InvariantCulture);

    public static string ToDbText(this DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);

    /// <summary>Escapes LIKE wildcards so a search term is matched literally (used with ESCAPE '\').</summary>
    public static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");

    /// <summary>Formats a lecturer's name from the title, first name and last name columns.</summary>
    public static string? GetLecturerName(this SqliteDataReader reader, int titleOrdinal) =>
        reader.IsDBNull(titleOrdinal)
            ? null
            : $"{reader.GetString(titleOrdinal)} {reader.GetString(titleOrdinal + 1)} {reader.GetString(titleOrdinal + 2)}";
}
