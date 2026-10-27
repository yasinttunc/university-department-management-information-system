using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Navigation;

namespace UniversityDepartmentMIS.Core.Tests;

public class ScreenCatalogTests
{
    [Fact]
    public void Admin_SeesEveryScreen()
    {
        Assert.Equal(Enum.GetValues<AppScreen>(), ScreenCatalog.VisibleTo(UserRole.Admin).Select(s => s.Screen));
    }

    [Fact]
    public void Staff_DoNotSeeAdminScreens()
    {
        var screens = ScreenCatalog.VisibleTo(UserRole.Staff).Select(s => s.Screen).ToList();

        Assert.DoesNotContain(AppScreen.Users, screens);
        Assert.DoesNotContain(AppScreen.DataImport, screens);
        Assert.Contains(AppScreen.Students, screens);
    }
}
