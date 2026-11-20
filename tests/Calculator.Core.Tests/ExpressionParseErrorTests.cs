namespace Calculator.Core.Tests;

[Trait("Feature", "F1")]
[Trait("Requirement", "R2")]
[Trait("Requirement", "R5")]
public sealed class ExpressionParseErrorTests
{
    [Theory]
    [InlineData("", ErrorCode.EmptyExpression, 0, 1)]
    [InlineData("   ", ErrorCode.EmptyExpression, 0, 1)]
    [InlineData("2 + a", ErrorCode.UnexpectedCharacter, 4, 1)]
    [InlineData("(1+2)", ErrorCode.UnexpectedCharacter, 0, 1)]
    [InlineData("1,5", ErrorCode.UnexpectedCharacter, 1, 1)]
    [InlineData("1e3", ErrorCode.UnexpectedCharacter, 1, 1)]
    [InlineData("1.2.3", ErrorCode.InvalidNumber, 0, 5)]
    [InlineData(".", ErrorCode.InvalidNumber, 0, 1)]
    [InlineData("79228162514264337593543950336", ErrorCode.Overflow, 0, 29)]
    [InlineData("10 +", ErrorCode.MissingOperand, 4, 1)]
    [InlineData("-", ErrorCode.MissingOperand, 1, 1)]
    [InlineData("10 * / 2", ErrorCode.MissingOperand, 5, 1)]
    [InlineData("* 3", ErrorCode.MissingOperand, 0, 1)]
    [InlineData("10 20", ErrorCode.MissingOperator, 3, 2)]
    public void Parse_InvalidInput_ThrowsWithCodePositionAndLength(
        string input, ErrorCode code, int position, int length)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Equal(code, exception.Code);
        Assert.Equal(position, exception.Position);
        Assert.Equal(length, exception.Length);
    }

    [Theory]
    [InlineData("2 + a", "a")]
    [InlineData("(1+2)", "parentheses")]
    [InlineData("1,5", "'.'")]
    public void Parse_InvalidInput_MessageMentionsHint(string input, string hint)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Contains(hint, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("\t", ErrorCode.EmptyExpression, 0, 1)]
    [InlineData("10 + ", ErrorCode.MissingOperand, 5, 1)]
    [InlineData("10 −", ErrorCode.MissingOperand, 4, 1)]
    [InlineData("1 2 3", ErrorCode.MissingOperator, 2, 1)]
    [InlineData("2 ^ 3", ErrorCode.UnexpectedCharacter, 2, 1)]
    [InlineData("1 000", ErrorCode.MissingOperator, 2, 3)]
    public void Parse_EdgeCaseInvalidInput_ThrowsWithCodePositionAndLength(
        string input, ErrorCode code, int position, int length)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Equal(code, exception.Code);
        Assert.Equal(position, exception.Position);
        Assert.Equal(length, exception.Length);
    }

    [Theory]
    [InlineData("1 .5", ErrorCode.MissingOperator, 2, 2)]
    [InlineData("* a", ErrorCode.MissingOperand, 0, 1)]
    [InlineData("10 * / 2 a", ErrorCode.MissingOperand, 5, 1)]
    public void Parse_MoreInvalidInput_ThrowsWithCodePositionAndLength(
        string input, ErrorCode code, int position, int length)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Equal(code, exception.Code);
        Assert.Equal(position, exception.Position);
        Assert.Equal(length, exception.Length);
    }

    [Theory]
    [Trait("Requirement", "R4")]
    [InlineData("0.00000000000000000000000000001")]
    [InlineData("1.00000000000000000000000000001")]
    public void Parse_LiteralNotExactlyRepresentable_ThrowsInvalidNumberMentioningExactly(string input)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Equal(ErrorCode.InvalidNumber, exception.Code);
        Assert.Equal(0, exception.Position);
        Assert.Equal(31, exception.Length);
        Assert.Contains("exactly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Parse_EmojiCharacter_ThrowsUnexpectedCharacterWithLengthTwoAndWellFormedMessage()
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse("2 + \U0001F600"));

        Assert.Equal(ErrorCode.UnexpectedCharacter, exception.Code);
        Assert.Equal(4, exception.Position);
        Assert.Equal(2, exception.Length);
        var normalized = exception.Message.Normalize(); // throws on a lone surrogate
        Assert.True(
            normalized.Contains("\U0001F600", StringComparison.Ordinal)
                || normalized.Contains("U+1F600", StringComparison.OrdinalIgnoreCase),
            $"Message should show the emoji or U+1F600: {exception.Message}");
    }

    [Theory]
    [InlineData("2 + \u202E", "U+202E", "\u202E")]
    [InlineData("2 + \0", "U+0000", "\0")]
    public void Parse_InvisibleOrControlCharacter_MessageShowsCodePointNotRawCharacter(
        string input, string codePoint, string rawCharacter)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Equal(ErrorCode.UnexpectedCharacter, exception.Code);
        Assert.Equal(4, exception.Position);
        Assert.Equal(1, exception.Length);
        Assert.Contains(codePoint, exception.Message, StringComparison.OrdinalIgnoreCase);
        // Ordinal: culture-aware comparison treats these characters as ignorable and would always match.
        Assert.DoesNotContain(rawCharacter, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_TenThousandCharacterInvalidNumber_ReportsWholeRunWithShortMessage()
    {
        var input = new string('1', 5000) + ".1." + new string('1', 5000);

        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Equal(ErrorCode.InvalidNumber, exception.Code);
        Assert.Equal(0, exception.Position);
        Assert.Equal(input.Length, exception.Length);
        Assert.True(exception.Message.Length < 200, $"Message length was {exception.Message.Length}.");
    }
}
