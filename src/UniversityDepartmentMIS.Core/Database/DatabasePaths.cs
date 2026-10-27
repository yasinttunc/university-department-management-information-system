namespace UniversityDepartmentMIS.Core.Database;

/// <summary>Works out where the database file lives.</summary>
public static class DatabasePaths
{
    /// <summary>Environment variable that overrides the default location (used by tests and CI).</summary>
    public const string OverrideVariable = "UDMIS_DB_PATH";

    public const string FileName = "university.db";

    /// <summary>
    /// Returns <c>UDMIS_DB_PATH</c> if it is set, otherwise
    /// <c>%LOCALAPPDATA%\UniversityDepartmentMIS\university.db</c>.
    /// </summary>
    public static string Resolve()
    {
        var overridePath = Environment.GetEnvironmentVariable(OverrideVariable);
        if (!string.IsNullOrWhiteSpace(overridePath))
        {
            return Path.GetFullPath(overridePath);
        }

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "UniversityDepartmentMIS", FileName);
    }
}
