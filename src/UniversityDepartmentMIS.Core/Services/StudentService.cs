using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Creates, updates and deletes students: validation, uniqueness checks and the advisor assignment.</summary>
public sealed class StudentService(
    IStudentRepository students, IStaffRepository staff, IAdvisingRepository advising, TimeProvider clock)
{
    public StudentService(IStudentRepository students, IStaffRepository staff, IAdvisingRepository advising)
        : this(students, staff, advising, TimeProvider.System)
    {
    }

    /// <summary>Adds a student and, if given, assigns their advisor.</summary>
    public OperationResult Create(Student student, long? advisorId)
    {
        var errors = Check(student with { Id = 0 }, advisorId);
        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        var id = students.Add(Normalise(student));
        if (advisorId is { } advisor)
        {
            advising.Assign(id, advisor);
        }

        return OperationResult.Success(id);
    }

    /// <summary>Saves changes to an existing student and, if given, changes their advisor.</summary>
    public OperationResult Update(Student student, long? advisorId)
    {
        ArgumentNullException.ThrowIfNull(student);
        if (students.Get(student.Id) is null)
        {
            return OperationResult.Failure(nameof(Student.Id), "This student no longer exists.");
        }

        var errors = Check(student, advisorId);
        if (errors.Count > 0)
        {
            return OperationResult.Failure(errors);
        }

        students.Update(Normalise(student));
        if (advisorId is { } advisor)
        {
            advising.Assign(student.Id, advisor);
        }

        return OperationResult.Success(student.Id);
    }

    public bool Delete(long studentId) => students.Delete(studentId);

    private List<ValidationError> Check(Student student, long? advisorId)
    {
        ArgumentNullException.ThrowIfNull(student);
        var normalised = Normalise(student);
        var errors = StudentValidator.Validate(normalised, clock.GetLocalNow().Year).ToList();

        if (errors.Count == 0)
        {
            foreach (var field in students.FindDuplicateFields(normalised))
            {
                errors.Add(new ValidationError(field, field switch
                {
                    nameof(Student.StudentNumber) => "Another student already has this student number.",
                    nameof(Student.NationalId) => "Another student already has this national ID.",
                    _ => "Another student already uses this email address.",
                }));
            }
        }

        if (advisorId is { } advisor && staff.Get(advisor) is null)
        {
            errors.Add(new ValidationError("Advisor", "Choose an advisor from the list."));
        }

        return errors;
    }

    private static Student Normalise(Student s) => s with
    {
        StudentNumber = s.StudentNumber?.Trim() ?? string.Empty,
        FirstName = s.FirstName?.Trim() ?? string.Empty,
        LastName = s.LastName?.Trim() ?? string.Empty,
        NationalId = s.NationalId?.Trim() ?? string.Empty,
        Email = s.Email?.Trim() ?? string.Empty,
        Phone = string.IsNullOrWhiteSpace(s.Phone) ? null : s.Phone.Trim(),
        Address = string.IsNullOrWhiteSpace(s.Address) ? null : s.Address.Trim(),
        Programme = s.Programme?.Trim() ?? string.Empty,
    };
}
