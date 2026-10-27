using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Core.Tests;

public class PasswordHasherTests
{
    // A low iteration count keeps these tests fast; the format and checks are the same.
    private const int FastIterations = 1_000;

    [Fact]
    public void Hash_UsesStoredFormatWithDefaultIterations()
    {
        var parts = PasswordHasher.Hash("Secret123").Split('$');

        Assert.Equal(4, parts.Length);
        Assert.Equal("pbkdf2", parts[0]);
        Assert.Equal("600000", parts[1]);
        Assert.Equal(16, Convert.FromBase64String(parts[2]).Length);
        Assert.Equal(32, Convert.FromBase64String(parts[3]).Length);
    }

    [Fact]
    public void Verify_AcceptsTheCorrectPassword()
    {
        var hash = PasswordHasher.Hash("Secret123", FastIterations);

        Assert.True(PasswordHasher.Verify("Secret123", hash));
    }

    [Theory]
    [InlineData("secret123")]
    [InlineData("Secret1234")]
    [InlineData("")]
    public void Verify_RejectsAWrongPassword(string attempt)
    {
        var hash = PasswordHasher.Hash("Secret123", FastIterations);

        Assert.False(PasswordHasher.Verify(attempt, hash));
    }

    [Fact]
    public void Hash_SamePasswordTwice_GivesDifferentSalts()
    {
        var first = PasswordHasher.Hash("Secret123", FastIterations);
        var second = PasswordHasher.Hash("Secret123", FastIterations);

        Assert.NotEqual(first, second);
        Assert.True(PasswordHasher.Verify("Secret123", first));
        Assert.True(PasswordHasher.Verify("Secret123", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Secret123")]
    [InlineData("pbkdf2$600000$onlythree")]
    [InlineData("sha256$1000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2$0$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2$-5$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2$1000$not base64!$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2$1000$AAAA$AAAA")]
    public void Verify_RejectsMalformedHashesWithoutThrowing(string stored)
    {
        Assert.False(PasswordHasher.Verify("Secret123", stored));
    }
}
