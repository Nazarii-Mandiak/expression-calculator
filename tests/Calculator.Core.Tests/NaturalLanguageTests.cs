namespace Calculator.Core.Tests;

[Trait("Feature", "F3")]
public sealed class NaturalLanguageTests
{
    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    [InlineData("What is 10 minus 2 multiplied by 6 divided by 4?", "10 - 2 * 6 / 4")]
    [InlineData("what's 3 plus 4", "3 + 4")]
    [InlineData("Calculate 2.5 times 4.", "2.5 * 4")]
    [InlineData("how much is 9 over 3", "9 / 3")]
    [InlineData("10 x 3", "10 * 3")]
    [InlineData("please compute 1 + 2", "1 + 2")]
    [InlineData("What is 10 minus", "10 -")]
    [InlineData("what is 2+2?", "2+2")]
    [InlineData("what is 10-2*6/4", "10-2*6/4")]
    [InlineData("what is -5 plus 3", "-5 + 3")]
    [InlineData("what is 2 \u00d7 3", "2 \u00d7 3")]
    [InlineData("What\u2019s 3 plus 4", "3 + 4")]
    [InlineData("what is 2 plus 3\n", "2 + 3")]
    [InlineData("what\u00a0is\u00a02\u00a0plus\u00a03", "2 + 3")]
    [InlineData("what is 1.2.3 plus 1", "1.2.3 + 1")]
    public void TryTranslate_SpecQuestion_ReturnsExpressionText(string text, string expected)
    {
        var translated = NaturalLanguage.TryTranslate(text, out var expression);

        Assert.True(translated);
        Assert.Equal(expected, expression);
    }

    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    [InlineData("what is the weather")]
    [InlineData("subtract 2 from 10")]
    [InlineData("ten plus two")]
    [InlineData("")]
    public void TryTranslate_UnsupportedQuestion_ReturnsFalseAndNull(string text)
    {
        var translated = NaturalLanguage.TryTranslate(text, out var expression);

        Assert.False(translated);
        Assert.Null(expression);
    }

    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    [InlineData("WHAT IS 2 PLUS 2?", "2 + 2")]
    [InlineData("What's 8 Divided By 2", "8 / 2")]
    [InlineData("CALCULATE 3 MULTIPLIED BY 5.", "3 * 5")]
    [InlineData("10 X 3", "10 * 3")]
    public void TryTranslate_MixedCase_IsCaseInsensitive(string text, string expected)
    {
        var translated = NaturalLanguage.TryTranslate(text, out var expression);

        Assert.True(translated);
        Assert.Equal(expected, expression);
    }

    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    [InlineData("what is  2   plus    2", "2 + 2")]
    [InlineData("  how much is 9 over 3  ", "9 / 3")]
    [InlineData("what is 6\tmultiplied   by 7", "6 * 7")]
    public void TryTranslate_ExtraWhitespace_CollapsesToSingleSpaces(string text, string expected)
    {
        var translated = NaturalLanguage.TryTranslate(text, out var expression);

        Assert.True(translated);
        Assert.Equal(expected, expression);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    public void TryTranslate_DecimalNumber_PassesThroughUnchanged()
    {
        var translated = NaturalLanguage.TryTranslate("what is 0.5 times 4", out var expression);

        Assert.True(translated);
        Assert.Equal("0.5 * 4", expression);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    public void TryTranslate_QuestionWithoutQuestionMark_ReturnsExpressionText()
    {
        var translated = NaturalLanguage.TryTranslate(
            "What is 10 minus 2 multiplied by 6 divided by 4", out var expression);

        Assert.True(translated);
        Assert.Equal("10 - 2 * 6 / 4", expression);
    }

    [Theory]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    [InlineData(" ")]
    [InlineData("what is")]
    [InlineData("?")]
    public void TryTranslate_NothingLeftAfterFillers_ReturnsFalseAndNull(string text)
    {
        var translated = NaturalLanguage.TryTranslate(text, out var expression);

        Assert.False(translated);
        Assert.Null(expression);
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    public void TryTranslate_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => NaturalLanguage.TryTranslate(null!, out _));
    }

    [Fact]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    public void TryTranslate_SimpleSum_NeverComputesResult()
    {
        var translated = NaturalLanguage.TryTranslate("what is 2 plus 2", out var expression);

        Assert.True(translated);
        Assert.Equal("2 + 2", expression);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    public void TryTranslate_BriefExampleThenParseAndEvaluate_ReturnsSeven()
    {
        var translated = NaturalLanguage.TryTranslate(
            "What is 10 minus 2 multiplied by 6 divided by 4?", out var expression);

        Assert.True(translated);
        Assert.NotNull(expression);
        Assert.Equal(7m, Expression.Parse(expression).Evaluate().Value);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R6")]
    [Trait("Requirement", "R9")]
    public void TryTranslate_SymbolRunThenParseAndEvaluate_ReturnsFour()
    {
        var translated = NaturalLanguage.TryTranslate("what is 2+2?", out var expression);

        Assert.True(translated);
        Assert.NotNull(expression);
        Assert.Equal(4m, Expression.Parse(expression).Evaluate().Value);
    }
}
