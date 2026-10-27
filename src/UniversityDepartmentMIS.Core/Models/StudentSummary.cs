namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A short view of an advisee, with the advisor's evaluation if one has been recorded.</summary>
public sealed record StudentSummary(
    long StudentId,
    string StudentNumber,
    string FirstName,
    string LastName,
    int YearOfStudy,
    Evaluation? Evaluation)
{
    public string FullName => $"{FirstName} {LastName}";
}
