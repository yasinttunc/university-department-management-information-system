using ClosedXML.Excel;
using UniversityDepartmentMIS.Core.Repositories;
using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class ExcelImportServiceTests : IDisposable
{
    private static readonly string[] Header = ExcelImportService.Columns.ToArray();

    private readonly TestDatabase _db = new();
    private readonly StudentRepository _students;
    private readonly ExcelImportService _service;

    public ExcelImportServiceTests()
    {
        _students = new StudentRepository(_db.Connections);
        _service = new ExcelImportService(_students, new TestClock(2026));
    }

    public void Dispose() => _db.Dispose();

    private static object[] Row(string number, string nationalId, string email, object? year = null, string first = "Alex") =>
        [number, first, "Morgan", nationalId, email, "", "", "Technology and Knowledge Management", year ?? 1, 2026];

    /// <summary>Builds an .xlsx in memory. Numbers are written as numbers, like Excel would store them.</summary>
    private static MemoryStream Workbook(string sheetName, string[] header, params object[][] rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(sheetName);
        for (var c = 0; c < header.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = header[c];
        }

        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                sheet.Cell(r + 2, c + 1).Value = rows[r][c] switch
                {
                    int i => i,
                    double d => d,
                    string s => s,
                    _ => Blank.Value,
                };
            }
        }

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public void Import_ValidRows_AreSaved()
    {
        using var file = Workbook("Students", Header,
            Row("26000001", "100000001", "alex.one@example.com"),
            Row("26000002", "100000002", "alex.two@example.com", year: 2));

        var report = _service.Import(file);

        Assert.Empty(report.FileErrors);
        Assert.Equal(2, report.Imported);
        Assert.Equal(67, _students.List().Count);
        Assert.Contains(_students.List(), s => s.StudentNumber == "26000002" && s.YearOfStudy == 2 && s.Phone is null);
    }

    [Fact]
    public void Preview_SavesNothing()
    {
        using var file = Workbook("Students", Header, Row("26000001", "100000001", "alex.one@example.com"));

        var report = _service.Preview(file);

        Assert.Equal(1, report.Ready);
        Assert.Equal(0, report.Imported);
        Assert.Equal(65, _students.List().Count);
    }

    [Fact]
    public void Import_NumbersStoredAsNumbers_AreReadAsDigits()
    {
        var row = Row("x", "x", "alex.one@example.com");
        row[0] = 26000001;
        row[3] = 100000001;
        using var file = Workbook("Students", Header, row);

        var report = _service.Import(file);

        Assert.Equal(1, report.Imported);
        Assert.Contains(_students.List(), s => s.StudentNumber == "26000001" && s.NationalId == "100000001");
    }

    [Fact]
    public void Import_MissingRequiredColumn_IsAFileError()
    {
        using var file = Workbook("Students", Header.Where(h => h != "National ID").ToArray(),
            ["26000001", "Alex", "Morgan", "alex@example.com", "", "", "TKM", 1, 2026]);

        var report = _service.Import(file);

        Assert.Contains("National ID", Assert.Single(report.FileErrors));
        Assert.Equal(65, _students.List().Count);
    }

    [Fact]
    public void Import_HeaderInAnyOrderAndCase_IsAccepted()
    {
        var header = Header.Reverse().Select(h => h.ToUpperInvariant()).ToArray();
        var values = Row("26000001", "100000001", "alex.one@example.com").Reverse().ToArray();
        using var file = Workbook("Students", header, values);

        Assert.Equal(1, _service.Import(file).Imported);
    }

    [Fact]
    public void Import_InvalidRow_IsReportedWithItsRowNumberAndSkipped()
    {
        using var file = Workbook("Students", Header,
            Row("26000001", "100000001", "alex.one@example.com"),
            Row("26000002", "12345", "not-an-email"),
            Row("26000003", "100000003", "alex.three@example.com", year: "first"));

        var report = _service.Import(file);

        Assert.Equal(1, report.Imported);
        Assert.Equal(new[] { 3, 4 }, report.Rows.Where(r => r.Status == ImportRowStatus.Invalid).Select(r => r.RowNumber));
        Assert.Equal(2, report.Rows.Single(r => r.RowNumber == 3).Problems.Count);
        Assert.Contains("Year of study", report.Rows.Single(r => r.RowNumber == 4).Problems[0]);
    }

    [Fact]
    public void Import_DuplicatesInDatabaseOrFile_AreSkipped()
    {
        var existing = _students.Get(1)!;
        using var file = Workbook("Students", Header,
            Row(existing.StudentNumber, "100000001", "new.one@example.com"),
            Row("26000002", "100000002", "dup@example.com"),
            Row("26000003", "100000003", "DUP@example.com"));

        var report = _service.Import(file);

        Assert.Equal(1, report.Imported);
        Assert.Equal(2, report.Duplicates);
        Assert.Equal(new[] { 2, 4 }, report.Rows.Where(r => r.Status == ImportRowStatus.Duplicate).Select(r => r.RowNumber));
    }

    [Fact]
    public void Import_BlankLinesAreIgnored_AndTurkishLettersKept()
    {
        using var file = Workbook("Students", Header,
            Row("26000001", "100000001", "s1@example.com", first: "Şükrü"),
            new object[Header.Length],
            Row("26000002", "100000002", "s2@example.com", first: "Gülçin"));

        var report = _service.Import(file);

        Assert.Equal(2, report.Imported);
        Assert.Equal(2, report.Rows.Count);
        Assert.Contains(_students.List(), s => s.FirstName == "Şükrü");
    }

    [Fact]
    public void Import_PrefersTheStudentsSheet()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Notes").Cell(1, 1).Value = "Ignore this sheet";
        var sheet = workbook.AddWorksheet("students");
        for (var c = 0; c < Header.Length; c++)
        {
            sheet.Cell(1, c + 1).Value = Header[c];
            sheet.Cell(2, c + 1).Value = Row("26000001", "100000001", "s1@example.com")[c] switch { int i => i, string s => s, _ => Blank.Value };
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        Assert.Equal(1, _service.Import(stream).Imported);
    }

    [Fact]
    public void Import_NotAnExcelFile_IsAFileError()
    {
        using var stream = new MemoryStream("this is not a workbook"u8.ToArray());

        var report = _service.Import(stream);

        Assert.Single(report.FileErrors);
        Assert.Empty(report.Rows);
    }

    [Fact]
    public void Template_HasTheExpectedHeaderAndAnImportableExample()
    {
        using var stream = new MemoryStream();
        ExcelImportService.CreateTemplate(stream);
        stream.Position = 0;

        var report = _service.Preview(stream);

        Assert.Empty(report.FileErrors);
        Assert.Equal(ImportRowStatus.Ready, Assert.Single(report.Rows).Status);
    }
}
