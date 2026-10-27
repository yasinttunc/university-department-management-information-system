using UniversityDepartmentMIS.Core.Security;

namespace UniversityDepartmentMIS.Core.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Abcdefg1")]
    [InlineData("Department2026")]
    [InlineData("Résumé2026")]
    public void Validate_AcceptsPasswordsThatMeetEveryRule(string password)
    {
        Assert.Empty(PasswordPolicy.Validate(password));
        Assert.True(PasswordPolicy.IsValid(password));
    }

    [Theory]
    [InlineData("Abc1", "at least 8")]
    [InlineData("abcdefg1", "uppercase")]
    [InlineData("ABCDEFG1", "lowercase")]
    [InlineData("Abcdefgh", "digit")]
    public void Validate_ExplainsTheBrokenRule(string password, string expectedFragment)
    {
        var problems = PasswordPolicy.Validate(password);

        var problem = Assert.Single(problems);
        Assert.Contains(expectedFragment, problem);
    }

    [Fact]
    public void Validate_RejectsPasswordsLongerThanTheMaximum()
    {
        var password = "Aa1" + new string('x', PasswordPolicy.MaxLength);

        Assert.Contains(PasswordPolicy.Validate(password), p => p.Contains($"at most {PasswordPolicy.MaxLength}"));
    }

    [Fact]
    public void Validate_NullOrEmpty_ReportsEveryRule()
    {
        Assert.Equal(4, PasswordPolicy.Validate(null).Count);
        Assert.Equal(4, PasswordPolicy.Validate(string.Empty).Count);
    }

    [Fact]
    public void Description_UsesTheSameLimitsAsTheChecks()
    {
        Assert.Contains($"{PasswordPolicy.MinLength} to {PasswordPolicy.MaxLength} characters", PasswordPolicy.Description);
    }
}
