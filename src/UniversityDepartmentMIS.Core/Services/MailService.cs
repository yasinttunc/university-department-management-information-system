using System.Globalization;
using System.Text;
using UniversityDepartmentMIS.Core.Models;

namespace UniversityDepartmentMIS.Core.Services;

/// <summary>Someone to email: a display name and an address.</summary>
public sealed record MailRecipient(string Name, string Email);

/// <summary>
/// Turns a recipient list into mailto: links and CSV. The app never sends mail itself: it opens the user's mail
/// program with the addresses in BCC, so students do not see each other's addresses.
/// </summary>
public static class MailService
{
    /// <summary>Mail programs and Windows cut long mailto: links; links are split to stay under this length.</summary>
    public const int MaxLinkLength = 1900;

    public static IReadOnlyList<MailRecipient> FromStudents(IEnumerable<Student> students) =>
        students.Select(s => new MailRecipient(s.FullName, s.Email)).ToList();

    /// <summary>
    /// One or more <c>mailto:?bcc=...</c> links that together contain every distinct address once, each link at most
    /// <see cref="MaxLinkLength"/> characters.
    /// </summary>
    public static IReadOnlyList<string> BuildMailtoLinks(IEnumerable<string> emails, string? subject = null)
    {
        ArgumentNullException.ThrowIfNull(emails);
        var prefix = "mailto:?" + (string.IsNullOrWhiteSpace(subject) ? string.Empty : "subject=" + Uri.EscapeDataString(subject.Trim()) + "&") + "bcc=";
        var addresses = emails.Select(e => e.Trim()).Where(e => e.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);

        var links = new List<string>();
        var current = new StringBuilder();
        foreach (var address in addresses)
        {
            var encoded = Uri.EscapeDataString(address).Replace("%40", "@", StringComparison.Ordinal);
            if (prefix.Length + encoded.Length > MaxLinkLength)
            {
                throw new ArgumentException($"The address '{address}' is too long for a mailto: link.", nameof(emails));
            }

            var separator = current.Length == 0 ? string.Empty : ",";
            if (prefix.Length + current.Length + separator.Length + encoded.Length > MaxLinkLength)
            {
                links.Add(prefix + current);
                current.Clear();
                separator = string.Empty;
            }

            current.Append(separator).Append(encoded);
        }

        if (current.Length > 0)
        {
            links.Add(prefix + current);
        }

        return links;
    }

    /// <summary>CSV with a header row; fields are quoted when they contain commas, quotes or line breaks.</summary>
    public static string ToCsv(IEnumerable<MailRecipient> recipients)
    {
        ArgumentNullException.ThrowIfNull(recipients);
        var csv = new StringBuilder("Name,Email\r\n");
        foreach (var r in recipients)
        {
            csv.Append(Escape(r.Name)).Append(',').Append(Escape(r.Email)).Append("\r\n");
        }

        return csv.ToString();
    }

    /// <summary>Addresses separated by "; ", ready to paste into a mail program's BCC field.</summary>
    public static string ToAddressList(IEnumerable<MailRecipient> recipients) =>
        string.Join("; ", recipients.Select(r => r.Email).Distinct(StringComparer.OrdinalIgnoreCase));

    private static string Escape(string value)
    {
        // A leading =, +, - or @ would make a spreadsheet treat the field as a formula.
        var safe = value.Length > 0 && "=+-@".Contains(value[0], StringComparison.Ordinal) ? "'" + value : value;
        return safe.IndexOfAny([',', '"', '\r', '\n']) >= 0
            ? "\"" + safe.Replace("\"", "\"\"", StringComparison.Ordinal) + "\""
            : safe;
    }
}
