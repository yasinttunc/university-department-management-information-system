using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;

namespace UniversityDepartmentMIS.Core.Tests;

/// <summary>
/// A fresh database file with the real schema and demo data, deleted after the test.
/// Tests never share a database, so they can run in any order.
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public TestDatabase(bool initialise = true)
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"udmis-test-{Guid.NewGuid():N}.db");
        Connections = new ConnectionFactory(Path);
        if (initialise)
        {
            DatabaseInitializer.EnsureCreated(Connections);
        }
    }

    public string Path { get; }

    public ConnectionFactory Connections { get; }

    public long Scalar(string sql)
    {
        using var connection = Connections.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt64(command.ExecuteScalar());
    }

    public void Execute(string sql)
    {
        using var connection = Connections.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose()
    {
        // Pooled connections keep the file open on Windows.
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(Path);
        }
        catch (IOException)
        {
            // A leftover file in the temp folder is harmless.
        }
    }
}
