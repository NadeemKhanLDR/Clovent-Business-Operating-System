using Clovent.Restaurant.Continuity;
using Xunit;

namespace Clovent.Restaurant.Tests;

public sealed class LocalReceiptNumberTests
{
    [Theory]
    [InlineData("POS-01", 1, "CONT-POS01-00001")]
    [InlineData("POS01", 42, "CONT-POS01-00042")]
    [InlineData("REG-3", 100000, "CONT-REG3-100000")]
    [InlineData("term", 5, "CONT-TERM-00005")]
    public void Generate_ValidInputs_FormatsExpectedString(string terminalCode, long sequence, string expected)
    {
        var result = LocalReceiptNumber.Generate(terminalCode, sequence);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Generate_NullOrEmptyTerminalCode_UsesFallbackDefault()
    {
        var result1 = LocalReceiptNumber.Generate(null!, 1);
        var result2 = LocalReceiptNumber.Generate("", 2);
        var result3 = LocalReceiptNumber.Generate("   ", 3);

        Assert.Equal("CONT-TERM-00001", result1);
        Assert.Equal("CONT-TERM-00002", result2);
        Assert.Equal("CONT-TERM-00003", result3);
    }

    [Fact]
    public void Generate_SpecialCharactersInTerminalCode_CleansesToSafeIdentifier()
    {
        var result = LocalReceiptNumber.Generate("POS#01 @Station!", 7);
        Assert.Equal("CONT-POS01STATION-00007", result);
    }

    [Theory]
    [InlineData("CONT-POS01-00001", "POS01", 1)]
    [InlineData("CONT-REG3-00123", "REG3", 123)]
    [InlineData("CONT-TERM-99999", "TERM", 99999)]
    public void TryParse_ValidFormat_ExtractsComponents(string input, string expectedTerminal, long expectedSequence)
    {
        var success = LocalReceiptNumber.TryParse(input, out var terminalCode, out var sequence);
        Assert.True(success);
        Assert.Equal(expectedTerminal, terminalCode);
        Assert.Equal(expectedSequence, sequence);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("INV-0001")]
    [InlineData("CONT-POS01")]
    [InlineData("CONT-POS01-NOTANUM")]
    [InlineData("EMERG-POS01-00001")]
    public void TryParse_InvalidFormat_ReturnsFalse(string? input)
    {
        var success = LocalReceiptNumber.TryParse(input, out var terminalCode, out var sequence);
        Assert.False(success);
        Assert.Null(terminalCode);
        Assert.Equal(0, sequence);
    }
}
