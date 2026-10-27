using Microsoft.Data.Sqlite;
using UniversityDepartmentMIS.Core.Database;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public sealed class UserRepository(ConnectionFactory connections) : IUserRepository
{
    private const string Select = "SELECT id, username, password_hash, role, department FROM users";

    public User? FindByUsername(string username)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + " WHERE username = @username COLLATE NOCASE", ("@username", username));
        return command.ReadSingleOrDefault(Map);
    }

    public IReadOnlyList<User> List()
    {
        using var connection = connections.Open();
        using var command = connection.Sql(Select + " ORDER BY username");
        return command.ReadAll(Map);
    }

    public long Add(string username, string passwordHash, UserRole role, string department)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "INSERT INTO users (username, password_hash, role, department) " +
            "VALUES (@username, @hash, @role, @department) RETURNING id",
            ("@username", username), ("@hash", passwordHash), ("@role", role.ToString()), ("@department", department));
        return (long)command.ExecuteScalar()!;
    }

    public bool UpdatePasswordHash(long userId, string passwordHash)
    {
        using var connection = connections.Open();
        using var command = connection.Sql(
            "UPDATE users SET password_hash = @hash WHERE id = @id", ("@hash", passwordHash), ("@id", userId));
        return command.ExecuteNonQuery() == 1;
    }

    private static User Map(SqliteDataReader r) => new(
        r.GetInt64(0), r.GetString(1), r.GetString(2), Enum.Parse<UserRole>(r.GetString(3)), r.GetString(4));
}
