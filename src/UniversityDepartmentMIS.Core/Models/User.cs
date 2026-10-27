namespace UniversityDepartmentMIS.Core.Models;

/// <summary>An application account. Only a PBKDF2 hash of the password is ever stored.</summary>
public sealed record User(long Id, string Username, string PasswordHash, UserRole Role, string Department);
