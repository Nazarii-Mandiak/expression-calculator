namespace Calculator.Core.Tests;

public sealed class BinaryOperatorExtensionsTests
{
    [Theory]
    [Trait("Requirement", "R2")]
    [Trait("Requirement", "R7")]
    [Trait("Feature", "F6")]
    [InlineData(BinaryOperator.Add, '+')]
    [InlineData(BinaryOperator.Subtract, '-')]
    [InlineData(BinaryOperator.Multiply, '*')]
    [InlineData(BinaryOperator.Divide, '/')]
    public void ToSymbol_DefinedOperator_ReturnsAsciiSymbol(BinaryOperator op, char expected)
    {
        Assert.Equal(expected, op.ToSymbol());
    }

    [Fact]
    [Trait("Requirement", "R2")]
    [Trait("Requirement", "R7")]
    [Trait("Feature", "F6")]
    public void ToSymbol_UndefinedOperator_Throws()
    {
        var exception = Assert.ThrowsAny<Exception>(() => ((BinaryOperator)99).ToSymbol());

        Assert.IsNotType<NotImplementedException>(exception);
    }
}
