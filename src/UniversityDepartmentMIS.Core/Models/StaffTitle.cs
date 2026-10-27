namespace UniversityDepartmentMIS.Core.Models;

/// <summary>Academic title of a staff member.</summary>
public enum StaffTitle
{
    Professor,
    AssociateProfessor,
    AssistantProfessor,
    Doctor,
    Lecturer,
}

/// <summary>Converts titles to and from the short form stored in the database ("Assoc. Prof.").</summary>
public static class StaffTitles
{
    private static readonly IReadOnlyDictionary<StaffTitle, string> Abbreviations = new Dictionary<StaffTitle, string>
    {
        [StaffTitle.Professor] = "Prof.",
        [StaffTitle.AssociateProfessor] = "Assoc. Prof.",
        [StaffTitle.AssistantProfessor] = "Asst. Prof.",
        [StaffTitle.Doctor] = "Dr.",
        [StaffTitle.Lecturer] = "Lecturer",
    };

    public static string ToAbbreviation(this StaffTitle title) => Abbreviations[title];

    public static StaffTitle Parse(string abbreviation)
    {
        foreach (var (title, text) in Abbreviations)
        {
            if (text == abbreviation)
            {
                return title;
            }
        }

        throw new FormatException($"Unknown staff title '{abbreviation}'.");
    }
}
