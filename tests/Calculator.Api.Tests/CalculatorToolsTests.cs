using ModelContextProtocol;

namespace Calculator.Api.Tests;

public sealed class CalculatorToolsTests
{
    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    public void EvaluateExpression_ChallengeExpression_ReturnsCanonicalResultAndSteps()
    {
        var response = CalculatorTools.EvaluateExpression("10 - 2 * 6 / 4");

        Assert.Equal("10 - 2 * 6 / 4", response.Expression);
        Assert.Equal("7", response.Result);
        Assert.NotNull(response.Steps);
        Assert.Equal(
            [new StepDto("2", "*", "6", "12"), new StepDto("12", "/", "4", "3"), new StepDto("10", "-", "3", "7")],
            response.Steps);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    [InlineData("10-2", "10 - 2", "8")]
    [InlineData("  1+2*3  ", "1 + 2 * 3", "7")]
    public void EvaluateExpression_UnformattedInput_ReturnsCanonicalExpression(string expression, string expectedCanonical, string expectedResult)
    {
        var response = CalculatorTools.EvaluateExpression(expression);

        Assert.Equal(expectedCanonical, response.Expression);
        Assert.Equal(expectedResult, response.Result);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    [InlineData("10 + * 3", "MissingOperand", 5, 1)]
    [InlineData("10 / 0", "DivisionByZero", 3, 1)]
    [InlineData("2 + a", "UnexpectedCharacter", 4, 1)]
    public void EvaluateExpression_InvalidExpression_ThrowsMcpExceptionWithCodeIndexAndLength(
        string expression, string expectedCode, int expectedPosition, int expectedLength)
    {
        var exception = Assert.Throws<McpException>(() => CalculatorTools.EvaluateExpression(expression));

        Assert.Contains(expectedCode, exception.Message, StringComparison.Ordinal);
        Assert.Contains($"index {expectedPosition}", exception.Message, StringComparison.Ordinal);
        Assert.Contains($"length {expectedLength}", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    public void EvaluateExpression_1001Characters_ThrowsMcpExceptionWithInputTooLongAtIndex1000()
    {
        var exception = Assert.Throws<McpException>(() => CalculatorTools.EvaluateExpression(new string('1', 1001)));

        Assert.Contains("InputTooLong", exception.Message, StringComparison.Ordinal);
        Assert.Contains("index 1000", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    public void ValidateExpression_1001Characters_ThrowsMcpExceptionWithInputTooLongAtIndex1000()
    {
        var exception = Assert.Throws<McpException>(() => CalculatorTools.ValidateExpression(new string('1', 1001)));

        Assert.Contains("InputTooLong", exception.Message, StringComparison.Ordinal);
        Assert.Contains("index 1000", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    public void EvaluateExpression_NullExpression_ThrowsMcpExceptionWithMissingExpression()
    {
        var exception = Assert.Throws<McpException>(() => CalculatorTools.EvaluateExpression(null!));

        Assert.Contains("MissingExpression", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    public void ValidateExpression_NullExpression_ThrowsMcpExceptionWithMissingExpression()
    {
        var exception = Assert.Throws<McpException>(() => CalculatorTools.ValidateExpression(null!));

        Assert.Contains("MissingExpression", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Feature", "F8")]
    [InlineData("10-2", "10 - 2")]
    [InlineData("10 / 0", "10 / 0")]
    [InlineData("79228162514264337593543950335 * 2", "79228162514264337593543950335 * 2")]
    public void ValidateExpression_ParsableExpression_ReturnsValidWithCanonicalAndNeverEvaluates(string expression, string expectedCanonical)
    {
        var response = CalculatorTools.ValidateExpression(expression);

        Assert.True(response.Valid);
        Assert.Equal(expectedCanonical, response.Canonical);
        Assert.Null(response.Error);
    }

    [Theory]
    [Trait("Requirement", "R9")]
    [Trait("Requirement", "R5")]
    [Trait("Feature", "F8")]
    [InlineData("10 + * 3", "MissingOperand", 5, 1)]
    [InlineData("2 + a", "UnexpectedCharacter", 4, 1)]
    [InlineData("10 20", "MissingOperator", 3, 2)]
    [InlineData("", "EmptyExpression", 0, 1)]
    public void ValidateExpression_InvalidExpression_ReturnsInvalidWithLocatedError(
        string expression, string expectedCode, int expectedPosition, int expectedLength)
    {
        var response = CalculatorTools.ValidateExpression(expression);

        Assert.False(response.Valid);
        Assert.Null(response.Canonical);
        Assert.NotNull(response.Error);
        Assert.Equal(expectedCode, response.Error.ErrorCode);
        Assert.Equal(expectedPosition, response.Error.Position);
        Assert.Equal(expectedLength, response.Error.Length);
        Assert.False(string.IsNullOrWhiteSpace(response.Error.Message));
    }
}
