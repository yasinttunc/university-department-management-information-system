using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Repositories;

public interface IStaffRepository
{
    /// <summary>Lists staff by last name, then first name.</summary>
    IReadOnlyList<StaffMember> List();

    StaffMember? Get(long id);

    /// <summary>Every staff member with their section count, weekly teaching hours and number of advisees.</summary>
    IReadOnlyList<StaffWorkload> ListWorkloads();
}
