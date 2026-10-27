using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Security;

/// <summary>Outcome of a sign-in attempt. A failure never says which of the two fields was wrong.</summary>
public sealed record AuthResult(bool Succeeded, User? User, string? Error)
{
    public const string InvalidCredentials = "Incorrect username or password.";

    public static AuthResult Success(User user) => new(true, user, null);

    public static AuthResult Failure() => new(false, null, InvalidCredentials);
}
