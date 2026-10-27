using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Security;

/// <summary>What each role may change. Everyone can view every screen they see in the menu.</summary>
public static class Permissions
{
    /// <summary>Adding, editing and deleting student records.</summary>
    public static bool CanEditStudents(UserRole role) => role == UserRole.Admin;

    /// <summary>Refilling a section's class list.</summary>
    public static bool CanAssignSections(UserRole role) => role == UserRole.Admin;

    /// <summary>Recording advisors' evaluations of their advisees.</summary>
    public static bool CanRecordEvaluations(UserRole role) => role is UserRole.Admin or UserRole.Staff;
}
