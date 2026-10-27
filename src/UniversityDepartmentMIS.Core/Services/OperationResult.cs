namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Outcome of a create or update: the record's id on success, otherwise the reasons it was refused.</summary>
public sealed record OperationResult(long? Id, IReadOnlyList<ValidationError> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public static OperationResult Success(long id) => new(id, []);

    public static OperationResult Failure(IReadOnlyList<ValidationError> errors) => new(null, errors);

    public static OperationResult Failure(string field, string message) => new(null, [new ValidationError(field, message)]);
}
