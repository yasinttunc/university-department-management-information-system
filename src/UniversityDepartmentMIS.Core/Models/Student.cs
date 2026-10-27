namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A student record. <see cref="NationalId"/> is a nine-digit identifier (fictional in the demo data).</summary>
public sealed record Student(
    long Id,
    string StudentNumber,
    string FirstName,
    string LastName,
    string NationalId,
    string Email,
    string? Phone,
    string? Address,
    string Programme,
    int YearOfStudy,
    int EnrolmentYear)
{
    public string FullName => $"{FirstName} {LastName}";
}
