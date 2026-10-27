using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Navigation;

namespace UniversityDepartmentMIS.Desktop.Navigation;

/// <summary>What every screen needs: the services, the signed-in user and a way to open another screen.</summary>
public sealed class ScreenContext(AppServices services, User user, Action<AppScreen, long?> navigate)
{
    public AppServices Services { get; } = services;

    public User User { get; } = user;

    /// <summary>Opens another screen, optionally preselecting a record (e.g. a staff member's id).</summary>
    public void Navigate(AppScreen screen, long? recordId = null) => navigate(screen, recordId);
}
