namespace UniversityDepartmentMIS.Core.Services;

/// <summary>A problem with one field of a form, e.g. ("Email", "Enter a valid email address.").</summary>
public sealed record ValidationError(string Field, string Message);
