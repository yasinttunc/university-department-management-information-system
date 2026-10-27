using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class UserServiceTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly UserRepository _users;
    private readonly UserService _service;

    public UserServiceTests()
    {
        _users = new UserRepository(_db.Connections);
        _service = new UserService(_users);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void Create_ValidUser_CanSignIn()
    {
        var result = _service.Create(" office.manager ", "Office2026", UserRole.Staff, "Technology and Knowledge Management");

        Assert.True(result.Succeeded);
        Assert.True(new AuthService(_users).Login("office.manager", "Office2026").Succeeded);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    public void Create_BadUsername_IsRefused(string username)
    {
        var result = _service.Create(username, "Office2026", UserRole.Staff, "Test");

        Assert.Equal("Username", Assert.Single(result.Errors).Field);
    }

    [Fact]
    public void Create_TakenUsername_IsRefusedIgnoringCase()
    {
        Assert.Contains(_service.Create("Admin", "Office2026", UserRole.Staff, "Test").Errors, e => e.Field == "Username");
    }

    [Fact]
    public void Create_WeakPassword_ListsEveryBrokenRule()
    {
        var result = _service.Create("newuser", "short", UserRole.Staff, "Test");

        Assert.All(result.Errors, e => Assert.Equal("Password", e.Field));
        Assert.Equal(PasswordPolicy.Validate("short").Count, result.Errors.Count);
        Assert.Equal(2, _users.List().Count);
    }

    [Fact]
    public void ResetPassword_ChangesTheHash()
    {
        var staff = _users.FindByUsername("staff")!;

        Assert.True(_service.ResetPassword(staff.Id, "NewStaff2026").Succeeded);

        var auth = new AuthService(_users);
        Assert.False(auth.Login("staff", "Staff2026").Succeeded);
        Assert.True(auth.Login("staff", "NewStaff2026").Succeeded);
    }

    [Fact]
    public void ResetPassword_WeakOrUnknownUser_IsRefused()
    {
        Assert.False(_service.ResetPassword(1, "weak").Succeeded);
        Assert.False(_service.ResetPassword(999, "Strong2026").Succeeded);
    }
}
