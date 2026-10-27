namespace UniversityDepartmentMIS.Core.Models;

/// <summary>One row of the student list: the student and the name of their advisor, if any.</summary>
public sealed record StudentListItem(
    long Id,
    string StudentNumber,
    string FirstName,
    string LastName,
    int YearOfStudy,
    string Email,
    long? AdvisorId,
    string? AdvisorName)
{
    public string FullName => $"{FirstName} {LastName}";
}
