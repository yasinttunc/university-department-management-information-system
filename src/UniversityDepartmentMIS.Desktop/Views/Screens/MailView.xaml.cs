using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using UniversityDepartmentMIS.Core.Models;
using UniversityDepartmentMIS.Core.Services;
using UniversityDepartmentMIS.Desktop.Navigation;

namespace UniversityDepartmentMIS.Desktop.Views.Screens;

/// <summary>
/// Builds a recipient list from a year of study or a section and hands it to the user's mail program (BCC),
/// the clipboard or a CSV file. Nothing is sent by the app itself.
/// </summary>
public partial class MailView : UserControl
{
    private const string ByYear = "Year of study";
    private const string BySection = "Section";

    private readonly ScreenContext _context;
    private List<RecipientRow> _rows = [];

    public MailView(ScreenContext context)
    {
        InitializeComponent();
        _context = context;
        SourcePicker.ItemsSource = new[] { ByYear, BySection };
        SourcePicker.SelectedIndex = 0;
    }

    private IEnumerable<MailRecipient> Selected => _rows.Where(r => r.IsSelected).Select(r => r.Recipient);

    private void SourcePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, SourcePicker))
        {
            return;
        }

        if (SourcePicker.SelectedItem as string == BySection)
        {
            GroupPicker.DisplayMemberPath = nameof(SectionWithOccupancy.Label);
            GroupPicker.ItemsSource = _context.Services.Courses.ListSectionsWithOccupancy();
        }
        else
        {
            GroupPicker.DisplayMemberPath = string.Empty;
            GroupPicker.ItemsSource = new[] { "All years", "Year 1", "Year 2", "Year 3", "Year 4" };
        }

        GroupPicker.SelectedIndex = 0;
    }

    private void GroupPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!ReferenceEquals(e.OriginalSource, GroupPicker))
        {
            return;
        }

        IEnumerable<Student> students = GroupPicker.SelectedItem switch
        {
            SectionWithOccupancy section => _context.Services.Enrolments.StudentsInSection(section.SectionId),
            string and not "All years" => _context.Services.Students.List(GroupPicker.SelectedIndex),
            string => _context.Services.Students.List(),
            _ => [],
        };

        _rows = MailService.FromStudents(students).Select(r => new RecipientRow(r, UpdateCount)).ToList();
        RecipientsGrid.ItemsSource = _rows;
        StatusText.Text = string.Empty;
        UpdateCount();
    }

    private void UpdateCount() => CountText.Text = $"{_rows.Count(r => r.IsSelected)} of {_rows.Count} selected";

    private void SetAll(bool selected)
    {
        foreach (var row in _rows)
        {
            row.IsSelected = selected;
        }
    }

    private void SelectAll_Click(object sender, RoutedEventArgs e) => SetAll(true);

    private void SelectNone_Click(object sender, RoutedEventArgs e) => SetAll(false);

    private bool HasSelection()
    {
        if (Selected.Any())
        {
            return true;
        }

        StatusText.Text = "Select at least one recipient.";
        return false;
    }

    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasSelection())
        {
            return;
        }

        var links = MailService.BuildMailtoLinks(Selected.Select(r => r.Email), SubjectInput.Text);
        if (links.Count > 1)
        {
            var answer = MessageBox.Show(Window.GetWindow(this)!,
                $"The list is too long for one message link, so {links.Count} draft messages will open, each with part of the list in BCC.",
                "Open in mail app", MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (answer != MessageBoxResult.OK)
            {
                return;
            }
        }

        try
        {
            foreach (var link in links)
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }

            StatusText.Text = $"Opened {links.Count} draft message(s) in your mail app.";
        }
        catch (Win32Exception)
        {
            StatusText.Text = "No mail app is set up on this computer. Use Copy addresses or Export CSV instead.";
        }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        if (HasSelection())
        {
            Clipboard.SetText(MailService.ToAddressList(Selected));
            StatusText.Text = "Addresses copied. Paste them into the BCC field.";
        }
    }

    private void ExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (!HasSelection())
        {
            return;
        }

        var dialog = new SaveFileDialog { Filter = "CSV file (*.csv)|*.csv", FileName = "recipients.csv" };
        if (dialog.ShowDialog(Window.GetWindow(this)) == true)
        {
            File.WriteAllText(dialog.FileName, MailService.ToCsv(Selected), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            StatusText.Text = $"Saved {dialog.FileName}.";
        }
    }

    /// <summary>A recipient with a check box; selected by default.</summary>
    public sealed class RecipientRow(MailRecipient recipient, Action changed) : INotifyPropertyChanged
    {
        private bool _isSelected = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public MailRecipient Recipient { get; } = recipient;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                changed();
            }
        }
    }
}
