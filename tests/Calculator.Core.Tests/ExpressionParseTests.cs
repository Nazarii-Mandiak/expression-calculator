namespace Calculator.Core.Tests;

[Trait("Feature", "F1")]
public sealed class ExpressionParseTests
{
    [Theory]
    [Trait("Requirement", "R2")]
    [InlineData("10-2*6/4", "10 - 2 * 6 / 4")]
    [InlineData("  10 - 2 * 6 / 4  ", "10 - 2 * 6 / 4")]
    [InlineData("42", "42")]
    [InlineData("3.50 + .5", "3.5 + 0.5")]
    [InlineData("5. * 2", "5 * 2")]
    [InlineData("2 × 3 ÷ 4 − 1", "2 * 3 / 4 - 1")]
    [InlineData("-5 + 3", "-5 + 3")]
    [InlineData("2 * -3", "2 * -3")]
    [InlineData("5 - - 3", "5 - -3")]
    [InlineData("--5", "5")]
    [InlineData("+5", "5")]
    [InlineData("1\t+\t2", "1 + 2")]
    public void Parse_ValidInput_ToStringReturnsCanonicalForm(string input, string expected)
    {
        var expression = Expression.Parse(input);

        Assert.Equal(expected, expression.ToString());
    }

    [Theory]
    [Trait("Requirement", "R2")]
    [InlineData("79228162514264337593543950335", "79228162514264337593543950335")]
    [InlineData("\t1+2 \t", "1 + 2")]
    [InlineData("2×3÷4−1", "2 * 3 / 4 - 1")]
    [InlineData("−5", "-5")]
    [InlineData("---5", "-5")]
    public void Parse_EdgeCaseValidInput_ToStringReturnsCanonicalForm(string input, string expected)
    {
        var expression = Expression.Parse(input);

        Assert.Equal(expected, expression.ToString());
    }

    [Theory]
    [Trait("Requirement", "R2")]
    [InlineData("-0", "0")]
    [InlineData("007", "7")]
    public void Parse_MoreValidInput_ToStringReturnsCanonicalForm(string input, string expected)
    {
        var expression = Expression.Parse(input);

        Assert.Equal(expected, expression.ToString());
    }

    [Fact]
    [Trait("Requirement", "R4")]
    public void Parse_LiteralWith28ExactDecimals_ToStringKeepsEveryDigit()
    {
        var expression = Expression.Parse("0.0000000000000000000000000001");

        Assert.Equal("0.0000000000000000000000000001", expression.ToString());
    }

    [Fact]
    [Trait("Requirement", "R2")]
    public void Parse_FiftyThousandOperands_ParsesWithoutError()
    {
        var input = string.Join(" + ", Enumerable.Repeat("1", 50_000));

        var expression = Expression.Parse(input);

        Assert.Equal(input, expression.ToString());
    }

    [Fact]
    [Trait("Requirement", "R2")]
    public void Parse_TenThousandUnaryMinusSigns_FoldsToPositiveNumber()
    {
        var input = new string('-', 10_000) + "5";

        var expression = Expression.Parse(input);

        Assert.Equal("5", expression.ToString());
    }

    [Fact]
    [Trait("Requirement", "R5")]
    public void Parse_NullText_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Expression.Parse(null!));
    }

    [Fact]
    [Trait("Requirement", "R5")]
    public void Parse_SyntaxErrorAfterDivisionByZero_ThrowsMissingOperandNotDivisionByZero()
    {
        var exception = Assert.Throws<CalculationException>(() => Expression.Parse("1/0 +"));

        Assert.Equal(ErrorCode.MissingOperand, exception.Code);
        Assert.Equal(5, exception.Position);
        Assert.Equal(1, exception.Length);
    }
}
