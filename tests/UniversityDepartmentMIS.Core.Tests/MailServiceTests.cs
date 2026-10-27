using UniversityDepartmentMIS.Core.Services;

namespace UniversityDepartmentMIS.Core.Tests;

public class MailServiceTests
{
    private static IEnumerable<string> Addresses(int count) =>
        Enumerable.Range(1, count).Select(i => $"student.number{i:000}@example.com");

    [Fact]
    public void BuildMailtoLinks_ShortList_IsOneLinkWithBcc()
    {
        var links = MailService.BuildMailtoLinks(["a@example.com", "b@example.com"], "Exam rooms");

        Assert.Equal("mailto:?subject=Exam%20rooms&bcc=a@example.com,b@example.com", Assert.Single(links));
    }

    [Fact]
    public void BuildMailtoLinks_LongList_IsSplitUnderTheLimit()
    {
        var links = MailService.BuildMailtoLinks(Addresses(200));

        Assert.True(links.Count > 1);
        Assert.All(links, link => Assert.True(link.Length <= MailService.MaxLinkLength));
        var all = links.SelectMany(l => l["mailto:?bcc=".Length..].Split(',')).ToList();
        Assert.Equal(Addresses(200), all);
    }

    [Fact]
    public void BuildMailtoLinks_DropsDuplicatesAndBlanks()
    {
        var links = MailService.BuildMailtoLinks(["a@example.com", "A@example.com", " ", "b@example.com"]);

        Assert.Equal("mailto:?bcc=a@example.com,b@example.com", Assert.Single(links));
    }

    [Fact]
    public void BuildMailtoLinks_EncodesSpecialCharacters()
    {
        var link = Assert.Single(MailService.BuildMailtoLinks(["a+b@example.com"], "Q&A"));

        Assert.Equal("mailto:?subject=Q%26A&bcc=a%2Bb@example.com", link);
    }

    [Fact]
    public void ToCsv_QuotesFieldsThatNeedIt()
    {
        var csv = MailService.ToCsv([new MailRecipient("Morgan, Alex", "alex@example.com"), new MailRecipient("Sam \"S\" Lee", "sam@example.com")]);

        Assert.Equal("Name,Email\r\n\"Morgan, Alex\",alex@example.com\r\n\"Sam \"\"S\"\" Lee\",sam@example.com\r\n", csv);
    }

    [Fact]
    public void ToCsv_NeutralisesSpreadsheetFormulas()
    {
        var csv = MailService.ToCsv([new MailRecipient("=HYPERLINK(\"x\")", "x@example.com")]);

        Assert.Contains("'=HYPERLINK", csv);
    }

    [Fact]
    public void ToAddressList_JoinsDistinctAddresses()
    {
        var list = MailService.ToAddressList([new MailRecipient("A", "a@example.com"), new MailRecipient("A again", "A@example.com"), new MailRecipient("B", "b@example.com")]);

        Assert.Equal("a@example.com; b@example.com", list);
    }
}
