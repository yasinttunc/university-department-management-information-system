using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Security;

/// <summary>Signs users in by checking the password against the stored PBKDF2 hash.</summary>
public sealed class AuthService(IUserRepository users)
{
    // Verified when the username does not exist, so unknown and known usernames take about the same time.
    private static readonly Lazy<string> DummyHash = new(() => PasswordHasher.Hash(Guid.NewGuid().ToString()));

    public AuthResult Login(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
        {
            return AuthResult.Failure();
        }

        var user = users.FindByUsername(username.Trim());
        if (user is null)
        {
            PasswordHasher.Verify(password, DummyHash.Value);
            return AuthResult.Failure();
        }

        return PasswordHasher.Verify(password, user.PasswordHash) ? AuthResult.Success(user) : AuthResult.Failure();
    }
}
