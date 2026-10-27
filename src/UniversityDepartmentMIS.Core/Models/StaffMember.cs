namespace UniversityDepartmentMIS.Core.Models;

/// <summary>A member of academic staff: lecturer and/or student advisor.</summary>
public sealed record StaffMember(long Id, StaffTitle Title, string FirstName, string LastName, string Email)
{
    public string FullName => $"{FirstName} {LastName}";

    public string DisplayName => $"{Title.ToAbbreviation()} {FirstName} {LastName}";

    /// <summary>The display name, so lists and screen readers show the person rather than the record.</summary>
    public override string ToString() => DisplayName;
}
