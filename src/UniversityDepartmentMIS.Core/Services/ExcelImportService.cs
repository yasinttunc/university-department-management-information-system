using System.Globalization;
using ClosedXML.Excel;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Repositories;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>One data row of the spreadsheet and what will happen to it.</summary>
public sealed record ImportRow(int RowNumber, Student Student, ImportRowStatus Status, IReadOnlyList<string> Problems);

public enum ImportRowStatus
{
    Ready,
    Duplicate,
    Invalid,
}

/// <summary>Result of reading (and optionally importing) a spreadsheet.</summary>
public sealed record ImportReport(IReadOnlyList<ImportRow> Rows, IReadOnlyList<string> FileErrors, int Imported)
{
    public int Ready => Rows.Count(r => r.Status == ImportRowStatus.Ready);

    public int Duplicates => Rows.Count(r => r.Status == ImportRowStatus.Duplicate);

    public int Invalid => Rows.Count(r => r.Status == ImportRowStatus.Invalid);
}

/// <summary>
/// Imports students from an .xlsx file. The sheet named "Students" (or else the first sheet) must have a header row
/// with the columns in <see cref="Columns"/>, in any order and any letter case. Each row is checked with
/// <see cref="StudentValidator"/>; rows whose student number, national ID or email already exist (in the database or
/// earlier in the file) are skipped. All valid rows are saved in one transaction.
/// </summary>
public sealed class ExcelImportService(IStudentRepository students, TimeProvider clock)
{
    public const string PreferredSheetName = "Students";

    public static IReadOnlyList<string> Columns { get; } =
    [
        "Student number", "First name", "Last name", "National ID", "Email", "Phone", "Address", "Programme",
        "Year of study", "Enrolment year",
    ];

    private static readonly HashSet<string> OptionalColumns = new(StringComparer.OrdinalIgnoreCase) { "Phone", "Address" };

    public ExcelImportService(IStudentRepository students)
        : this(students, TimeProvider.System)
    {
    }

    /// <summary>Reads the file and reports what would be imported, without saving anything.</summary>
    public ImportReport Preview(Stream workbook) => Read(workbook);

    /// <summary>Reads the file and saves every row with status <see cref="ImportRowStatus.Ready"/>.</summary>
    public ImportReport Import(Stream workbook)
    {
        var report = Read(workbook);
        if (report.FileErrors.Count > 0)
        {
            return report;
        }

        var ready = report.Rows.Where(r => r.Status == ImportRowStatus.Ready).Select(r => r.Student).ToList();
        var imported = ready.Count == 0 ? 0 : students.AddRange(ready);
        return report with { Imported = imported };
    }

    /// <summary>Writes an empty workbook with the expected header row and one example row.</summary>
    public static void CreateTemplate(Stream output)
    {
        ArgumentNullException.ThrowIfNull(output);
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(PreferredSheetName);
        for (var i = 0; i < Columns.Count; i++)
        {
            sheet.Cell(1, i + 1).Value = Columns[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }

        string[] example =
        [
            "26000001", "Alex", "Morgan", "123456789", "alex.morgan@example.com", "+44 7700 900123",
            "1 Example Road, Swansea", "Technology and Knowledge Management", "1", "2026",
        ];
        for (var i = 0; i < example.Length; i++)
        {
            sheet.Cell(2, i + 1).Value = example[i];
        }

        sheet.Columns().Width = 22;
        workbook.SaveAs(output);
    }

    private ImportReport Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return new ImportReport([], [$"The file could not be read as an Excel workbook (.xlsx): {ex.Message}"], 0);
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault(w =>
                string.Equals(w.Name.Trim(), PreferredSheetName, StringComparison.OrdinalIgnoreCase)) ?? workbook.Worksheets.FirstOrDefault();
            if (sheet is null || sheet.LastRowUsed() is null)
            {
                return new ImportReport([], ["The workbook is empty."], 0);
            }

            var header = sheet.FirstRowUsed()!;
            var columnIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in header.CellsUsed())
            {
                columnIndex.TryAdd(Text(cell), cell.Address.ColumnNumber);
            }

            var missing = Columns.Where(c => !OptionalColumns.Contains(c) && !columnIndex.ContainsKey(c)).ToList();
            if (missing.Count > 0)
            {
                return new ImportReport([], [$"Missing column(s): {string.Join(", ", missing)}. Expected: {string.Join(", ", Columns)}."], 0);
            }

            var existing = students.List();
            var numbers = new HashSet<string>(existing.Select(s => s.StudentNumber));
            var nationalIds = new HashSet<string>(existing.Select(s => s.NationalId));
            var emails = new HashSet<string>(existing.Select(s => s.Email), StringComparer.OrdinalIgnoreCase);
            var year = clock.GetLocalNow().Year;

            var rows = new List<ImportRow>();
            var lastRow = sheet.LastRowUsed()!.RowNumber();
            for (var rowNumber = header.RowNumber() + 1; rowNumber <= lastRow; rowNumber++)
            {
                var row = sheet.Row(rowNumber);
                string Get(string column) => columnIndex.TryGetValue(column, out var index) ? Text(row.Cell(index)) : string.Empty;

                if (Columns.All(c => Get(c).Length == 0))
                {
                    continue; // blank line between data rows
                }

                var problems = new List<string>();
                var yearOfStudy = ParseInt(Get("Year of study"), "Year of study", problems);
                var enrolmentYear = ParseInt(Get("Enrolment year"), "Enrolment year", problems);
                var student = new Student(
                    0, Get("Student number"), Get("First name"), Get("Last name"), Get("National ID"), Get("Email"),
                    NullIfEmpty(Get("Phone")), NullIfEmpty(Get("Address")), Get("Programme"), yearOfStudy, enrolmentYear);

                if (problems.Count == 0)
                {
                    problems.AddRange(StudentValidator.Validate(student, year).Select(e => e.Message));
                }

                if (problems.Count > 0)
                {
                    rows.Add(new ImportRow(rowNumber, student, ImportRowStatus.Invalid, problems));
                    continue;
                }

                var duplicates = new List<string>();
                if (!numbers.Add(student.StudentNumber))
                {
                    duplicates.Add($"Student number {student.StudentNumber} already exists.");
                }

                if (!nationalIds.Add(student.NationalId))
                {
                    duplicates.Add("National ID already exists.");
                }

                if (!emails.Add(student.Email))
                {
                    duplicates.Add($"Email {student.Email} already exists.");
                }

                rows.Add(duplicates.Count > 0
                    ? new ImportRow(rowNumber, student, ImportRowStatus.Duplicate, duplicates)
                    : new ImportRow(rowNumber, student, ImportRowStatus.Ready, []));
            }

            return new ImportReport(rows, [], 0);
        }
    }

    private static string Text(IXLCell cell)
    {
        var value = cell.Value;
        if (value.IsBlank)
        {
            return string.Empty;
        }

        if (value.IsNumber)
        {
            return value.GetNumber().ToString("0.##########", CultureInfo.InvariantCulture);
        }

        return value.IsText ? value.GetText().Trim() : cell.GetFormattedString().Trim();
    }

    private static int ParseInt(string text, string column, List<string> problems)
    {
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value))
        {
            return value;
        }

        problems.Add($"{column} must be a whole number.");
        return 0;
    }

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;
}
