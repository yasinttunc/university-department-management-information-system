using System.Text.RegularExpressions;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Creates accounts and resets passwords, applying <see cref="PasswordPolicy"/>.</summary>
public sealed partial class UserService(IUserRepository users)
{
    public IReadOnlyList<User> List() => users.List();

    public OperationResult Create(string? username, string? password, UserRole role, string department)
    {
        var name = username?.Trim() ?? string.Empty;
        var errors = new List<ValidationError>();

        if (!UsernamePattern().IsMatch(name))
        {
            errors.Add(new ValidationError("Username", "Use 3 to 32 letters, digits, dots, hyphens or underscores."));
        }
        else if (users.FindByUsername(name) is not null)
        {
            errors.Add(new ValidationError("Username", "This username is already taken."));
        }

        errors.AddRange(PasswordPolicy.Validate(password).Select(message => new ValidationError("Password", message)));
        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        return OperationResult.Success(users.Add(name, PasswordHasher.Hash(password!), role, department));
    }

    public OperationResult ResetPassword(long userId, string? newPassword)
    {
        var problems = PasswordPolicy.Validate(newPassword);
        if (problems.Count > 0)
        {
            return OperationResult.Failure(problems.Select(message => new ValidationError("Password", message)).ToList());
        }

        return users.UpdatePasswordHash(userId, PasswordHasher.Hash(newPassword!))
            ? OperationResult.Success(userId)
            : OperationResult.Failure("User", "This user no longer exists.");
    }

    [GeneratedRegex(@"^[A-Za-z0-9._-]{3,32}$")]
    private static partial Regex UsernamePattern();
}
