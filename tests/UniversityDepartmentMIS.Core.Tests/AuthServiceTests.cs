using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Core.Tests;

public class AuthServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly AuthService _auth;

    public AuthServiceTests() => _auth = new AuthService(new UserRepository(_db.Connections));

    public void Dispose() => _db.Dispose();

    [Theory]
    [InlineData("admin", "Admin2026", UserRole.Admin)]
    [InlineData("staff", "Staff2026", UserRole.Staff)]
    public void Login_WithDemoAccount_Succeeds(string username, string password, UserRole expectedRole)
    {
        var result = _auth.Login(username, password);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(expectedRole, result.User!.Role);
    }

    [Fact]
    public void Login_IgnoresUsernameCaseAndSpaces()
    {
        Assert.True(_auth.Login("  ADMIN ", "Admin2026").Succeeded);
    }

    [Fact]
    public void Login_WithWrongPassword_FailsWithGenericMessage()
    {
        var result = _auth.Login("admin", "admin2026");

        Assert.False(result.Succeeded);
        Assert.Null(result.User);
        Assert.Equal(AuthResult.InvalidCredentials, result.Error);
    }

    [Fact]
    public void Login_WithUnknownUser_FailsWithTheSameMessage()
    {
        Assert.Equal(_auth.Login("admin", "wrong").Error, _auth.Login("nobody", "Admin2026").Error);
    }

    [Theory]
    [InlineData(null, "Admin2026")]
    [InlineData("", "Admin2026")]
    [InlineData("admin", null)]
    [InlineData("admin", "")]
    [InlineData("admin", "1")]
    public void Login_WithMissingOrShortcutCredentials_Fails(string? username, string? password)
    {
        Assert.False(_auth.Login(username, password).Succeeded);
    }

    [Fact]
    public void Database_StoresNoPlaintextPasswords()
    {
        var users = new UserRepository(_db.Connections).List();

        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.StartsWith("pbkdf2$600000$", u.PasswordHash));
        Assert.DoesNotContain(users, u => u.PasswordHash.Contains("2026"));
    }
}
