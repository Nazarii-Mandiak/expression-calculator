namespace Calculator.Core.Tests;

[Trait("Feature", "Hardening")]
public sealed class MutationGapTests
{
    [Fact]
    [Trait("Requirement", "R5")]
    public void Parse_InvalidLiteralOfTwentyChars_EchoesItInFull()
    {
        const string literal = "1.1.1.1.1.1.1.1.1.11";

        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(literal));

        Assert.Equal(20, literal.Length);
        Assert.Contains(literal, exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("…", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    public void Parse_InvalidLiteralOfTwentyOneChars_TruncatesEchoWithEllipsis()
    {
        const string literal = "1.1.1.1.1.1.1.1.1.111";

        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(literal));

        Assert.Equal(21, literal.Length);
        Assert.DoesNotContain(literal, exception.Message, StringComparison.Ordinal);
        Assert.Contains("…", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R4")]
    [Trait("Requirement", "R5")]
    public void Parse_LeadingDotLiteralWithTooManyDecimals_ThrowsInvalidNumberExactly()
    {
        var exception = Assert.Throws<CalculationException>(
            () => Expression.Parse(".00000000000000000000000000001"));

        Assert.Equal(ErrorCode.InvalidNumber, exception.Code);
        Assert.Equal(0, exception.Position);
        Assert.Equal(30, exception.Length);
        Assert.Contains("exactly", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Requirement", "R5")]
    public void Parse_LoneSurrogate_ThrowsUnexpectedCharacterShownAsCodePoint()
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse("2 + \uD800"));

        Assert.Equal(ErrorCode.UnexpectedCharacter, exception.Code);
        Assert.Equal(4, exception.Position);
        Assert.Equal(1, exception.Length);
        Assert.Contains("U+D800", exception.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("\uD800", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("Requirement", "R6")]
    [InlineData("what is 2 multiplied")]
    [InlineData("what is 2 divided")]
    [InlineData("what is 2 multiplied 3")]
    [InlineData("what is 2a plus 1")]
    [InlineData("what is 3b")]
    [InlineData("what is 2 plus 3??")]
    public void TryTranslate_UnrecognisedWording_ReturnsFalse(string text)
    {
        var translated = NaturalLanguage.TryTranslate(text, out var expression);

        Assert.False(translated);
        Assert.Null(expression);
    }

    [Theory]
    [Trait("Requirement", "R5")]
    [InlineData("10 20", "expected an operator")]
    [InlineData("1.2.3", "not a valid number")]
    [InlineData("10 +", "expected a number after '+'")]
    [InlineData("* 3", "must start with a number")]
    [InlineData("79228162514264337593543950336", "too large")]
    public void Parse_InvalidInput_MessageContainsKeyPhrase(string input, string phrase)
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse(input));

        Assert.Contains(phrase, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [Trait("Requirement", "R5")]
    [InlineData("10 / 0", "division by zero")]
    [InlineData("79228162514264337593543950335 + 1", "too large")]
    public void Evaluate_ArithmeticFailure_MessageContainsKeyPhrase(string input, string phrase)
    {
        var expression = Expression.Parse(input);

        var exception = Assert.Throws<CalculationException>(() => expression.Evaluate());

        Assert.Contains(phrase, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Requirement", "R2")]
    public void ToSymbol_UndefinedOperator_MessageSaysUndefined()
    {
        var exception = Assert.ThrowsAny<Exception>(() => ((BinaryOperator)99).ToSymbol());

        Assert.Contains("Undefined", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
