using Microsoft.Data.Sqlite;

namespace UniversityDepartmentMIS.Core.Database;

/// <summary>Creates the schema and loads the demo data into an empty database.</summary>
public static class DatabaseInitializer
{
    /// <summary>Value written to <c>PRAGMA user_version</c> by schema.sql.</summary>
    public const int SchemaVersion = 1;

    private const string SchemaResource = "UniversityDepartmentMIS.Core.Database.schema.sql";
    private const string SeedResource = "UniversityDepartmentMIS.Core.Database.seed.sql";

    /// <summary>
    /// Applies schema.sql and seed.sql in one transaction if the database has no tables yet.
    /// A database that already has tables is never modified.
    /// </summary>
    /// <returns><c>true</c> if the database was created, <c>false</c> if it already existed.</returns>
    /// <exception cref="InvalidOperationException">The file holds tables but is not a database of this version.</exception>
    public static bool EnsureCreated(ConnectionFactory factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        using var connection = factory.Open();

        if (CountTables(connection) > 0)
        {
            var version = ReadUserVersion(connection);
            if (version != SchemaVersion)
            {
                throw new InvalidOperationException(
                    $"'{factory.DatabasePath}' is not a University Department MIS database " +
                    $"(schema version {version}, expected {SchemaVersion}).");
            }

            return false;
        }

        using var transaction = connection.BeginTransaction();
        Execute(connection, transaction, ReadResource(SchemaResource));
        Execute(connection, transaction, ReadResource(SeedResource));
        transaction.Commit();
        return true;
    }

    internal static string ReadResource(string name)
    {
        using var stream = typeof(DatabaseInitializer).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Embedded resource '{name}' is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static long CountTables(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type = 'table'";
        return (long)command.ExecuteScalar()!;
    }

    private static long ReadUserVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version";
        return (long)command.ExecuteScalar()!;
    }

    private static void Execute(SqliteConnection connection, SqliteTransaction transaction, string sql)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
