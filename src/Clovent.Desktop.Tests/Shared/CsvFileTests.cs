using Clovent.Desktop.Shared;
using Xunit;

namespace Clovent.Desktop.Tests.Shared;

public class CsvFileTests
{
    [Fact]
    public void FormatRow_PlainFields_JoinsWithCommasUnquoted()
    {
        var result = CsvFile.FormatRow(["Alpha", "Beta", "123"]);

        Assert.Equal("Alpha,Beta,123", result);
    }

    [Fact]
    public void FormatRow_FieldContainingComma_IsQuoted()
    {
        var result = CsvFile.FormatRow(["Acme, Inc.", "Beta"]);

        Assert.Equal("\"Acme, Inc.\",Beta", result);
    }

    [Fact]
    public void FormatRow_FieldContainingQuote_EscapesQuoteAndWraps()
    {
        var result = CsvFile.FormatRow(["Say \"hi\"", "Beta"]);

        Assert.Equal("\"Say \"\"hi\"\"\",Beta", result);
    }

    [Fact]
    public void ParseRow_PlainFields_SplitsOnCommas()
    {
        var result = CsvFile.ParseRow("Alpha,Beta,123");

        Assert.Equal(["Alpha", "Beta", "123"], result);
    }

    [Fact]
    public void ParseRow_QuotedFieldWithComma_KeepsCommaInsideField()
    {
        var result = CsvFile.ParseRow("\"Acme, Inc.\",Beta");

        Assert.Equal(["Acme, Inc.", "Beta"], result);
    }

    [Fact]
    public void ParseRow_EscapedQuote_UnescapesToSingleQuote()
    {
        var result = CsvFile.ParseRow("\"Say \"\"hi\"\"\",Beta");

        Assert.Equal(["Say \"hi\"", "Beta"], result);
    }

    [Fact]
    public void FormatThenParse_RoundTripsExactly()
    {
        string[] fields = ["Acme, Inc.", "Say \"hi\"", "plain", ""];

        var formatted = CsvFile.FormatRow(fields);
        var parsed = CsvFile.ParseRow(formatted);

        Assert.Equal(fields, parsed);
    }

    [Fact]
    public void ParseDataRows_SkipsHeaderAndBlankLines()
    {
        string[] lines = ["Header1,Header2", "A,1", "", "B,2"];

        var result = CsvFile.ParseDataRows(lines);

        Assert.Equal(2, result.Count);
        Assert.Equal(["A", "1"], result[0]);
        Assert.Equal(["B", "2"], result[1]);
    }

    [Fact]
    public void ParseDataRows_OnlyHeader_ReturnsEmpty()
    {
        string[] lines = ["Header1,Header2"];

        var result = CsvFile.ParseDataRows(lines);

        Assert.Empty(result);
    }
}
