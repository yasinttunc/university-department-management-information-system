using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface IUserRepository
{
    /// <summary>Finds a user by name, ignoring case.</summary>
    User? FindByUsername(string username);

    IReadOnlyList<User> List();

    long Add(string username, string passwordHash, UserRole role, string department);

    bool UpdatePasswordHash(long userId, string passwordHash);
}
