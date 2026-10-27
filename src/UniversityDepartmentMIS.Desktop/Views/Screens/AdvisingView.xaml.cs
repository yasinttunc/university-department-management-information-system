using System.Windows.Controls;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Security;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

public partial class AdvisingView : UserControl
{
    private readonly ScreenContext _context;

    public AdvisingView(ScreenContext context, long? advisorId)
    {
        InitializeComponent();
        _context = context;

        var staff = context.Services.Staff.List();
        AdvisorPicker.ItemsSource = staff;
        AdvisorPicker.SelectedItem = staff.FirstOrDefault(s => s.Id == advisorId) ?? staff.FirstOrDefault();
    }

    private void AdvisorPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ReferenceEquals(e.OriginalSource, AdvisorPicker))
        {
            Load();
        }
    }

    private void Load()
    {
        if (AdvisorPicker.SelectedItem is not StaffMember advisor)
        {
            AdviseesGrid.ItemsSource = null;
            return;
        }

        var canEdit = Permissions.CanRecordEvaluations(_context.User.Role);
        var rows = _context.Services.Advising.ForAdvisor(advisor.Id)
            .Select(s => new AdviseeRow(s, canEdit, Save))
            .ToList();
        AdviseesGrid.ItemsSource = rows;
        UpdateSummary(rows);
    }

    private void Save(AdviseeRow row)
    {
        _context.Services.Advising.SetEvaluation(row.Student.StudentId, row.Choice.Value);
        if (AdviseesGrid.ItemsSource is List<AdviseeRow> rows)
        {
            UpdateSummary(rows);
        }
    }

    private void UpdateSummary(IReadOnlyCollection<AdviseeRow> rows)
    {
        var evaluated = rows.Count(r => r.Choice.Value is not null);
        SummaryText.Text = rows.Count == 0
            ? "No advisees"
            : $"{rows.Count} advisees, {evaluated} evaluated, {rows.Count - evaluated} still to evaluate";
    }
}

/// <summary>An option in the evaluation drop-down; <see cref="Value"/> is null for "not evaluated yet".</summary>
public sealed record EvaluationChoice(Evaluation? Value, string Label)
{
    public static IReadOnlyList<EvaluationChoice> All { get; } =
    [
        new(null, "Not evaluated yet"),
        new(Evaluation.Successful, "Successful"),
        new(Evaluation.Average, "Average"),
        new(Evaluation.Unsuccessful, "Unsuccessful"),
    ];

    public static EvaluationChoice For(Evaluation? value) => All.First(c => c.Value == value);
}

/// <summary>One advisee in the grid. Choosing a different evaluation saves it straight away.</summary>
public sealed class AdviseeRow(StudentSummary student, bool canEdit, Action<AdviseeRow> save)
{
    private EvaluationChoice _choice = EvaluationChoice.For(student.Evaluation);

    public StudentSummary Student { get; } = student;

    public bool CanEdit { get; } = canEdit;

    public IReadOnlyList<EvaluationChoice> Choices => EvaluationChoice.All;

    public EvaluationChoice Choice
    {
        get => _choice;
        set
        {
            if (value is null || value == _choice)
            {
                return;
            }

            _choice = value;
            save(this);
        }
    }
}
