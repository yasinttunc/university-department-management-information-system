using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Services;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

/// <summary>Admin screen: preview an Excel file of students, then import the valid rows in one go.</summary>
public partial class DataImportView : UserControl
{
    private readonly ScreenContext _context;
    private string? _file;

    public DataImportView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;
        HelpText.Text =
            $"Import students from an Excel workbook (.xlsx). Use a sheet named \"{ExcelImportService.PreferredSheetName}\" (or the first sheet) " +
            $"with a header row: {string.Join(", ", ExcelImportService.Columns)}. Phone and Address may be left empty. " +
            "Rows with problems or with a student number, national ID or email that already exists are skipped.";
    }

    private Window Owner => Window.GetWindow(this)!;

    private void ChooseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx" };
        if (dialog.ShowDialog(Owner) != true)
        {
            return;
        }

        _file = dialog.FileName;
        FileText.Text = Path.GetFileName(_file);
        Show(Run(service => service.Preview), imported: false);
    }

    private void ImportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_file is null)
        {
            return;
        }

        var answer = MessageBox.Show(Owner, "Import the rows marked Ready?", "Import students",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer == MessageBoxResult.Yes)
        {
            Show(Run(service => service.Import), imported: true);
        }
    }

    private ImportReport? Run(Func<ExcelImportService, Func<Stream, ImportReport>> action)
    {
        try
        {
            using var stream = File.OpenRead(_file!);
            return action(_context.Services.ExcelImport)(stream);
        }
        catch (IOException ex)
        {
            SummaryText.Text = $"The file could not be opened: {ex.Message} Close it in Excel and try again.";
            RowsGrid.ItemsSource = null;
            ImportButton.IsEnabled = false;
            return null;
        }
    }

    private void Show(ImportReport? report, bool imported)
    {
        if (report is null)
        {
            return;
        }

        RowsGrid.ItemsSource = report.Rows.Select(r => new RowView(r)).ToList();
        if (report.FileErrors.Count > 0)
        {
            SummaryText.Text = string.Join(" ", report.FileErrors);
            ImportButton.IsEnabled = false;
            return;
        }

        SummaryText.Text = imported
            ? $"Imported {report.Imported} student(s). Skipped {report.Duplicates} duplicate(s) and {report.Invalid} row(s) with problems."
            : $"{report.Ready} row(s) ready to import, {report.Duplicates} duplicate(s), {report.Invalid} with problems.";
        ImportButton.IsEnabled = !imported && report.Ready > 0;
    }

    private void TemplateButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = "students-template.xlsx" };
        if (dialog.ShowDialog(Owner) == true)
        {
            using var stream = File.Create(dialog.FileName);
            ExcelImportService.CreateTemplate(stream);
            SummaryText.Text = $"Template saved to {dialog.FileName}.";
        }
    }

    /// <summary>An import row with its problems joined for display.</summary>
    public sealed record RowView(int RowNumber, ImportRowStatus Status, Student Student, string ProblemText)
    {
        public RowView(ImportRow row)
            : this(row.RowNumber, row.Status, row.Student, string.Join(" ", row.Problems))
        {
        }
    }
}
