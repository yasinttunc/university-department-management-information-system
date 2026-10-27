using System.Globalization;
using System.Security.Cryptography;

namespace UniversityDepartmentMIS.Core.Security;

/// <summary>
/// Salted PBKDF2-HMAC-SHA256 password hashing. Hashes are stored as
/// <c>pbkdf2$&lt;iterations&gt;$&lt;base64 salt&gt;$&lt;base64 key&gt;</c>, so the iteration count can be raised later
/// without breaking existing hashes.
/// </summary>
public static class PasswordHasher
{
    /// <summary>OWASP's 2023 recommendation for PBKDF2-HMAC-SHA256.</summary>
    public const int DefaultIterations = 600_000;

    private const string Scheme = "pbkdf2";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int MaxIterations = 10_000_000;

    public static string Hash(string password) => Hash(password, DefaultIterations);

    internal static string Hash(string password, int iterations)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentOutOfRangeException.ThrowIfLessThan(iterations, 1);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
        return string.Join('$', Scheme, iterations.ToString(CultureInfo.InvariantCulture),
            Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    /// <summary>
    /// Checks a password against a stored hash in constant time. Returns <c>false</c> (never throws) for a
    /// wrong password or a stored value that is not a valid hash.
    /// </summary>
    public static bool Verify(string password, string storedHash)
    {
        if (password is null || !TryParse(storedHash, out var iterations, out var salt, out var expected))
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static bool TryParse(string? storedHash, out int iterations, out byte[] salt, out byte[] key)
    {
        iterations = 0;
        salt = [];
        key = [];

        var parts = storedHash?.Split('$');
        if (parts is not { Length: 4 } || parts[0] != Scheme)
        {
            return false;
        }

        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations)
            || iterations < 1 || iterations > MaxIterations)
        {
            return false;
        }

        try
        {
            salt = Convert.FromBase64String(parts[2]);
            key = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length >= SaltSize && key.Length >= KeySize;
    }
}
