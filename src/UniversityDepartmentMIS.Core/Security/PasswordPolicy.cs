namespace UniversityDepartmentMIS.Core.Security;

/// <summary>
/// Rules for new passwords. They are checked when a password is created or changed, never at sign-in.
/// Messages are built from the same constants the checks use, so the two cannot drift apart.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 64;

    public static string Description =>
        $"Use {MinLength} to {MaxLength} characters, including an uppercase letter, a lowercase letter and a digit.";

    /// <summary>Returns one message per broken rule; an empty list means the password is acceptable.</summary>
    public static IReadOnlyList<string> Validate(string? password)
    {
        var problems = new List<string>();
        password ??= string.Empty;

        if (password.Length < MinLength)
        {
            problems.Add($"Must be at least {MinLength} characters long.");
        }

        if (password.Length > MaxLength)
        {
            problems.Add($"Must be at most {MaxLength} characters long.");
        }

        if (!password.Any(char.IsUpper))
        {
            problems.Add("Must contain an uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            problems.Add("Must contain a lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            problems.Add("Must contain a digit.");
        }

        return problems;
    }

    public static bool IsValid(string? password) => Validate(password).Count == 0;
}
