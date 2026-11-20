using System.Collections.Concurrent;
using System.Globalization;

namespace Calculator.Core.Tests;

[Trait("Feature", "F2")]
public sealed class ExpressionEvaluateTests
{
    public static TheoryData<string, EvaluationStep[]> StepCases => new()
    {
        {
            "10-2*6/4",
            [
                new(2m, BinaryOperator.Multiply, 6m, 12m),
                new(12m, BinaryOperator.Divide, 4m, 3m),
                new(10m, BinaryOperator.Subtract, 3m, 7m),
            ]
        },
        {
            "2+3*4",
            [
                new(3m, BinaryOperator.Multiply, 4m, 12m),
                new(2m, BinaryOperator.Add, 12m, 14m),
            ]
        },
        {
            "10-5-2",
            [
                new(10m, BinaryOperator.Subtract, 5m, 5m),
                new(5m, BinaryOperator.Subtract, 2m, 3m),
            ]
        },
        {
            "100/10/5",
            [
                new(100m, BinaryOperator.Divide, 10m, 10m),
                new(10m, BinaryOperator.Divide, 5m, 2m),
            ]
        },
        { "7", [] },
    };

    [Theory]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R4")]
    [InlineData("10-2*6/4", "7")]
    [InlineData("2+3*4", "14")]
    [InlineData("10-5-2", "3")]
    [InlineData("100/10/5", "2")]
    [InlineData("2*3/4", "1.5")]
    [InlineData("1+2+3+4+5", "15")]
    [InlineData("0.1+0.2", "0.3")]
    [InlineData("-5+3", "-2")]
    [InlineData("2*-3", "-6")]
    [InlineData("5--3", "8")]
    [InlineData("7", "7")]
    public void Evaluate_ValidExpression_ReturnsExactValue(string input, string expected)
    {
        var evaluation = Expression.Parse(input).Evaluate();

        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), evaluation.Value);
    }

    [Theory]
    [Trait("Requirement", "R3")]
    [MemberData(nameof(StepCases), DisableDiscoveryEnumeration = true)]
    public void Evaluate_ValidExpression_ReturnsStepsInEvaluationOrder(string input, EvaluationStep[] expected)
    {
        var evaluation = Expression.Parse(input).Evaluate();

        Assert.Equal(expected, evaluation.Steps);
    }

    [Theory]
    [Trait("Requirement", "R5")]
    [InlineData("10 / 0", ErrorCode.DivisionByZero, 3, 1)]
    [InlineData("1 + 5 / 0.0", ErrorCode.DivisionByZero, 6, 1)]
    [InlineData("79228162514264337593543950335 + 1", ErrorCode.Overflow, 30, 1)]
    [InlineData("79228162514264337593543950335 * 2", ErrorCode.Overflow, 30, 1)]
    [InlineData("79228162514264337593543950335 / 0.5", ErrorCode.Overflow, 30, 1)]
    [InlineData("-79228162514264337593543950335 - 1", ErrorCode.Overflow, 31, 1)]
    [InlineData("5 / -0", ErrorCode.DivisionByZero, 2, 1)]
    public void Evaluate_ArithmeticFailure_ThrowsWithCodePositionAndLength(
        string input, ErrorCode code, int position, int length)
    {
        var expression = Expression.Parse(input);

        var exception = Assert.Throws<CalculationException>(() => expression.Evaluate());

        Assert.Equal(code, exception.Code);
        Assert.Equal(position, exception.Position);
        Assert.Equal(length, exception.Length);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    [Trait("Requirement", "R5")]
    public void Evaluate_ResultBelowDecimalPrecision_UnderflowsToZeroWithoutError()
    {
        var evaluation = Expression.Parse("0.0000000000000001 * 0.0000000000000001").Evaluate();

        Assert.Equal(0m, evaluation.Value);
    }

    [Fact]
    [Trait("Requirement", "R3")]
    public void Evaluate_SameInstanceInParallel_AlwaysReturnsSameValueAndSteps()
    {
        var expression = Expression.Parse("10-2*6/4");
        EvaluationStep[] expectedSteps =
        [
            new(2m, BinaryOperator.Multiply, 6m, 12m),
            new(12m, BinaryOperator.Divide, 4m, 3m),
            new(10m, BinaryOperator.Subtract, 3m, 7m),
        ];
        var results = new ConcurrentBag<Evaluation>();

        Parallel.For(0, 1_000, _ => results.Add(expression.Evaluate()));

        Assert.Equal(1_000, results.Count);
        Assert.All(results, evaluation =>
        {
            Assert.Equal(7m, evaluation.Value);
            Assert.Equal(expectedSteps, evaluation.Steps);
        });
    }
}
